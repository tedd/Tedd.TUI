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
    private System.Collections.Generic.Dictionary<DependencyObject, System.Collections.Generic.Dictionary<DependencyProperty, object?>>? _triggerOriginalValues;
    private System.Collections.Generic.Dictionary<DependencyObject, System.Collections.Generic.Dictionary<DependencyProperty, object?>>? _triggerActiveValues;

    // Cached lists to avoid allocations during hot path evaluation
    private System.Collections.Generic.HashSet<(DependencyObject, DependencyProperty)>? _newlyActiveProperties;
    private System.Collections.Generic.List<(DependencyObject, DependencyProperty)>? _propertiesToRevert;
    private System.Collections.Generic.HashSet<TriggerBase>? _activeTriggers;

    private bool _isEvaluatingTriggers;

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TemplateProperty)
        {
            ApplyTemplate();
        }

        EvaluateTriggers();
    }

    private void EvaluateTriggers()
    {
        if (_isEvaluatingTriggers)
            return;

        bool hasTriggers = Template != null && Template.Triggers.Count > 0;

        // If there are no active trigger values and no triggers to evaluate, skip entirely
        if (!hasTriggers && (_triggerActiveValues == null || _triggerActiveValues.Count == 0))
        {
            _activeTriggers?.Clear();
            return;
        }

        _isEvaluatingTriggers = true;

        try
        {
            if (_newlyActiveProperties == null) _newlyActiveProperties = new();
            else _newlyActiveProperties.Clear();

            if (_activeTriggers == null) _activeTriggers = new();

            if (hasTriggers)
            {
                foreach (var triggerBase in Template!.Triggers)
                {
                    if (triggerBase is Trigger trigger && trigger.Property != null)
                    {
                        var currentValue = GetValue(trigger.Property);
                        bool isActive = object.Equals(currentValue, trigger.Value);
                        bool wasActive = _activeTriggers.Contains(triggerBase);

                        if (isActive)
                        {
                            if (!wasActive)
                            {
                                _activeTriggers.Add(triggerBase);
                            }

                            foreach (var setter in trigger.Setters)
                            {
                                if (setter.Property != null)
                                {
                                    DependencyObject target = this;

                                    // Resolve TargetName if specified
                                    if (!string.IsNullOrEmpty(setter.TargetName) && TemplateRoot != null)
                                    {
                                        var foundTarget = TemplateRoot.FindName(setter.TargetName);
                                        if (foundTarget != null)
                                        {
                                            target = foundTarget;
                                        }
                                    }

                                    if (_triggerOriginalValues == null) _triggerOriginalValues = new();
                                    if (!_triggerOriginalValues.TryGetValue(target, out var targetOriginals))
                                    {
                                        targetOriginals = new();
                                        _triggerOriginalValues[target] = targetOriginals;
                                    }

                                    if (_triggerActiveValues == null) _triggerActiveValues = new();
                                    if (!_triggerActiveValues.TryGetValue(target, out var targetActives))
                                    {
                                        targetActives = new();
                                        _triggerActiveValues[target] = targetActives;
                                    }

                                    // If the trigger is newly active for this pass, and we haven't stored an original value, store it
                                    if (!wasActive && !targetOriginals.ContainsKey(setter.Property))
                                    {
                                        targetOriginals[setter.Property] = target is UIElement uiElement && uiElement.HasLocalValue(setter.Property)
                                            ? target.GetValue(setter.Property)
                                            : DependencyProperty.UnsetValue;
                                    }

                                    // Apply the setter value ONLY IF newly active, OR we need to maintain it.
                                    // Actually, if the user explicitly overrode it, we shouldn't constantly re-apply it.
                                    // If it was already active, we shouldn't `SetValue` again if it matches or if the user overrode it.
                                    if (!wasActive)
                                    {
                                        target.SetValue(setter.Property, setter.Value);
                                        targetActives[setter.Property] = setter.Value;
                                    }

                                    _newlyActiveProperties.Add((target, setter.Property));
                                }
                            }
                        }
                        else
                        {
                            if (wasActive)
                            {
                                _activeTriggers.Remove(triggerBase);
                            }
                        }
                    }
                }
            }

            // Revert properties that are no longer active
            if (_triggerActiveValues != null)
            {
                if (_propertiesToRevert == null) _propertiesToRevert = new();
                else _propertiesToRevert.Clear();

                foreach (var kvpTarget in _triggerActiveValues)
                {
                    var target = kvpTarget.Key;
                    foreach (var kvpProp in kvpTarget.Value)
                    {
                        var prop = kvpProp.Key;
                        if (!_newlyActiveProperties.Contains((target, prop)))
                        {
                            _propertiesToRevert.Add((target, prop));
                        }
                    }
                }

                foreach (var (target, prop) in _propertiesToRevert)
                {
                    object? lastActiveValue = null;
                    if (_triggerActiveValues.TryGetValue(target, out var targetActives))
                    {
                        if (targetActives.TryGetValue(prop, out lastActiveValue))
                        {
                            targetActives.Remove(prop);
                            if (targetActives.Count == 0)
                                _triggerActiveValues.Remove(target);
                        }
                    }

                    if (_triggerOriginalValues != null && _triggerOriginalValues.TryGetValue(target, out var targetOriginals))
                    {
                        if (targetOriginals.TryGetValue(prop, out var originalValue))
                        {
                            targetOriginals.Remove(prop);
                            if (targetOriginals.Count == 0)
                                _triggerOriginalValues.Remove(target);

                            var currentValue = target.GetValue(prop);

                            // If the current value is NOT the value we set via the trigger, it means the user explicitly modified it locally.
                            // In a real WPF precedence system, LocalValue wins over Trigger, but since we modify local value,
                            // we must preserve the user's manual override by NOT restoring the original.
                            bool userModified = !object.Equals(currentValue, lastActiveValue);

                            if (!userModified)
                            {
                                if (originalValue == DependencyProperty.UnsetValue)
                                {
                                    target.ClearValue(prop);
                                }
                                else
                                {
                                    target.SetValue(prop, originalValue);
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
