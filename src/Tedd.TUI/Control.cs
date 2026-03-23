using System;

namespace Tedd.TUI;

public class Control : UIElement
{
    protected UIElement? TemplateRoot { get; private set; }

    public static readonly DependencyProperty TemplateProperty =
        DependencyProperty.Register("Template", typeof(ControlTemplate), typeof(Control), null);

    public ControlTemplate Template
    {
        get => (ControlTemplate)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register("Padding", typeof(Thickness), typeof(Control), new Thickness(0));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register("BorderBrush", typeof(ConsoleColor), typeof(Control), ConsoleColor.Gray);

    public ConsoleColor BorderBrush
    {
        get => (ConsoleColor)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(Control), new Thickness(0));

    public Thickness BorderThickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    // Store original values when a trigger setter is applied, to revert them when trigger condition becomes false

    // Cached lists to avoid allocations during hot path evaluation
    private System.Collections.Generic.List<(DependencyObject, DependencyProperty)>? _propertiesToRevert;
    private System.Collections.Generic.HashSet<TriggerBase>? _activeTriggers;

    // Set of dependency properties watched by at least one trigger in the current template; null when empty
    private System.Collections.Generic.HashSet<DependencyProperty>? _watchedTriggerProperties;

    private bool _isEvaluatingTriggers;

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TemplateProperty)
        {
            ApplyTemplate();
            RebuildWatchedTriggerProperties();
            EvaluateTriggers();
            return;
        }

        // Short-circuit: skip evaluation when dp is not watched by any trigger condition
        if (_watchedTriggerProperties == null || !_watchedTriggerProperties.Contains(dp))
            return;

        EvaluateTriggers();
    }

    private void RebuildWatchedTriggerProperties()
    {
        var template = Template;
        if (template == null || template.Triggers.Count == 0)
        {
            _watchedTriggerProperties = null;
            return;
        }

        if (_watchedTriggerProperties == null)
            _watchedTriggerProperties = new();
        else
            _watchedTriggerProperties.Clear();

        foreach (var triggerBase in template.Triggers)
        {
            if (triggerBase is Trigger trigger && trigger.Property != null)
                _watchedTriggerProperties.Add(trigger.Property);
        }
    }

    private void EvaluateTriggers()
    {
        if (_isEvaluatingTriggers)
            return;

        bool hasTriggers = Template != null && Template.Triggers.Count > 0;

        _isEvaluatingTriggers = true;

        try
        {
            if (_activeTriggers == null) _activeTriggers = new();


            if (_propertiesToRevert == null) _propertiesToRevert = new();

            foreach (var (target, prop) in _propertiesToRevert)
            {
                target.ClearTriggerValue(prop);
            }
            _propertiesToRevert.Clear();
            _activeTriggers.Clear();

            if (hasTriggers)
            {
                foreach (var triggerBase in Template!.Triggers)
                {
                    if (triggerBase is Trigger trigger && trigger.Property != null)
                    {
                        var currentValue = GetValue(trigger.Property);
                        bool isActive = object.Equals(currentValue, trigger.Value);

                        if (isActive)
                        {
                            _activeTriggers.Add(triggerBase);

                            foreach (var setter in trigger.Setters)
                            {
                                if (setter.Property != null)
                                {
                                    DependencyObject target = this;

                                    if (!string.IsNullOrEmpty(setter.TargetName) && TemplateRoot != null)
                                    {
                                        var foundTarget = TemplateRoot.FindName(setter.TargetName);
                                        if (foundTarget != null)
                                        {
                                            target = foundTarget;
                                        }
                                    }

                                    target.SetTriggerValue(setter.Property, setter.Value);
                                    _propertiesToRevert.Add((target, setter.Property));
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            _isEvaluatingTriggers = false;
        }
    }

    public virtual void ApplyTemplate()
    {
        var template = Template;
        if (template != null)
        {
            // Remove old template root parent
            if (TemplateRoot != null)
            {
                TemplateRoot.Parent = null;
                TemplateRoot.TemplatedParent = null;
            }

            TemplateRoot = template.LoadContent(this);

            if (TemplateRoot != null)
            {
                TemplateRoot.TemplatedParent = this;
                TemplateRoot.Parent = this; // Set logical/visual parent
                Invalidate();
            }
        }
        else
        {
            if (TemplateRoot != null)
            {
                TemplateRoot.Parent = null;
                TemplateRoot.TemplatedParent = null;
            }
            TemplateRoot = null;
            Invalidate();
        }
    }

    public override int VisualChildrenCount => TemplateRoot != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (TemplateRoot != null && index == 0) return TemplateRoot;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TemplateRoot != null)
        {
            Thickness padding = Padding;
            int paddingWidth = padding.Left + padding.Right;
            int paddingHeight = padding.Top + padding.Bottom;

            Size innerAvailableSize = new Size(
                System.Math.Max(0, availableSize.Width - paddingWidth),
                System.Math.Max(0, availableSize.Height - paddingHeight)
            );

            TemplateRoot.Measure(innerAvailableSize);

            return new Size(
                TemplateRoot.DesiredSize.Width + paddingWidth,
                TemplateRoot.DesiredSize.Height + paddingHeight
            );
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (TemplateRoot != null)
        {
            Thickness padding = Padding;
            int paddingWidth = padding.Left + padding.Right;
            int paddingHeight = padding.Top + padding.Bottom;

            int innerWidth = System.Math.Max(0, finalSize.Width - paddingWidth);
            int innerHeight = System.Math.Max(0, finalSize.Height - paddingHeight);

            TemplateRoot.Arrange(new Rect(padding.Left, padding.Top, innerWidth, innerHeight));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (TemplateRoot != null)
        {
            int x = RenderSize.X + offsetX;
            int y = RenderSize.Y + offsetY;
            TemplateRoot.Render(buffer, x, y);
        }
    }
}
