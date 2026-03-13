using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ShadUI;

namespace Bloomdo.Client.UI.Controls;

/// <summary>
/// A panel that reveals a delete action when swiped left.
/// Properly distinguishes horizontal swipe from vertical scroll.
/// Works on Android (no Cursor, no layout mutations during attach).
/// </summary>
public class SwipeRevealPanel : ContentControl
{
    private const double ActionPanelWidth = 80;
    private const double SnapThreshold = 0.35;
    private const double DirectionLockAngle = 1.2;

    private TranslateTransform _contentTranslate = new();
    private Border? _actionBorder;
    private Point _startPoint;
    private bool _isTracking;
    private bool _directionLocked;
    private bool _isHorizontal;
    private bool _isOpen;
    private bool _wrapped;

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, ICommand?>(nameof(ActionCommand));

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, object?>(nameof(ActionCommandParameter));

    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    public static readonly StyledProperty<bool> IsActionEnabledProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, bool>(nameof(IsActionEnabled), true);

    public bool IsActionEnabled
    {
        get => GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    private static SwipeRevealPanel? _currentlyOpenPanel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Intercept the first time real card content is set from AXAML.
        // Defer to next tick so we never mutate Content during a layout pass.
        if (change.Property == ContentProperty && !_wrapped)
        {
            var ctrl = change.NewValue as Control;
            if (ctrl is not null)
            {
                _wrapped = true;
                Dispatcher.UIThread.Post(() => WrapContent(ctrl));
            }
        }
    }

    private void WrapContent(Control child)
    {
        _contentTranslate = new TranslateTransform
        {
            Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.XProperty,
                    Duration = TimeSpan.FromMilliseconds(220),
                    Easing = new CubicEaseOut()
                }
            }
        };
        child.RenderTransform = _contentTranslate;

        var deleteIcon = new PathIcon
        {
            Data = Icons.Cross,
            Foreground = Brushes.White,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var deleteLabel = new TextBlock
        {
            Text = "Delete",
            Foreground = Brushes.White,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var deleteStack = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { deleteIcon, deleteLabel }
        };

        var deleteButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = deleteStack,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        deleteButton.Click += OnDeleteButtonClick;

        _actionBorder = new Border
        {
            Width = ActionPanelWidth,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = new SolidColorBrush(Color.Parse("#E53935")),
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Opacity = 0,
            Child = deleteButton
        };

        // Detach child from its current visual parent (ContentPresenter) before
        // reparenting it into the wrapper Panel. Without this, Avalonia throws
        // "Control already has a visual parent" which crashes on Android as JavaProxyThrowable.
        Content = null;

        var root = new Panel { ClipToBounds = true };
        root.Children.Add(_actionBorder);
        root.Children.Add(child);

        Content = root;
    }

    private void OnDeleteButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!IsActionEnabled) return;

        var cmd = ActionCommand;
        var param = ActionCommandParameter;
        if (cmd?.CanExecute(param) == true)
            cmd.Execute(param);

        AnimateClose();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed) return;

        if (_currentlyOpenPanel is not null && !ReferenceEquals(_currentlyOpenPanel, this))
            _currentlyOpenPanel.AnimateClose();

        _startPoint = e.GetPosition(this);
        _isTracking = true;
        _directionLocked = false;
        _isHorizontal = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isTracking) return;

        var current = e.GetPosition(this);
        var dx = current.X - _startPoint.X;
        var dy = current.Y - _startPoint.Y;

        if (!_directionLocked)
        {
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 10) return;

            _directionLocked = true;
            _isHorizontal = Math.Abs(dx) > Math.Abs(dy) * DirectionLockAngle;

            if (!_isHorizontal)
            {
                _isTracking = false;
                return;
            }

            e.Pointer.Capture(this);
        }

        if (!_isHorizontal) return;

        var newOffset = Math.Clamp(_isOpen ? -ActionPanelWidth + dx : dx, -ActionPanelWidth, 0);
        _contentTranslate.X = newOffset;

        if (_actionBorder is not null)
            _actionBorder.Opacity = Math.Abs(newOffset) / ActionPanelWidth;

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isTracking || !_isHorizontal)
        {
            _isTracking = false;
            return;
        }

        _isTracking = false;
        e.Pointer.Capture(null);

        if (Math.Abs(_contentTranslate.X) > ActionPanelWidth * SnapThreshold)
            AnimateOpen();
        else
            AnimateClose();

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_isTracking && _isHorizontal)
        {
            if (Math.Abs(_contentTranslate.X) > ActionPanelWidth * SnapThreshold)
                AnimateOpen();
            else
                AnimateClose();
        }

        _isTracking = false;
    }

    private void AnimateOpen()
    {
        _contentTranslate.X = -ActionPanelWidth;

        _actionBorder?
            .Animate(OpacityProperty)
            .From(_actionBorder.Opacity)
            .To(1.0)
            .WithDuration(TimeSpan.FromMilliseconds(220))
            .WithEasing(new CubicEaseOut())
            .Start();

        _isOpen = true;
        _currentlyOpenPanel = this;
    }

    public void AnimateClose()
    {
        _contentTranslate.X = 0.0;

        _actionBorder?
            .Animate(OpacityProperty)
            .From(_actionBorder.Opacity)
            .To(0.0)
            .WithDuration(TimeSpan.FromMilliseconds(220))
            .WithEasing(new CubicEaseOut())
            .Start();

        _isOpen = false;
        if (ReferenceEquals(_currentlyOpenPanel, this))
            _currentlyOpenPanel = null;
    }
}

