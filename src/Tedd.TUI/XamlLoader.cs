using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;

namespace Tedd.TUI;

public static class XamlLoader
{
    public static UIElement Load(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return ParseElement(doc.DocumentElement);
    }

    private static UIElement ParseElement(XmlElement element)
    {
        string typeName = "Tedd.TUI." + element.Name;
        Type type = Type.GetType(typeName);
        if (type == null)
        {
            throw new InvalidOperationException($"Type {typeName} not found.");
        }

        var instance = (UIElement)Activator.CreateInstance(type);

        foreach (XmlAttribute attr in element.Attributes)
        {
            SetProperty(instance, attr.Name, attr.Value);
        }

        // Handle Children
        if (instance is StackPanel stack)
        {
            foreach (XmlNode childNode in element.ChildNodes)
            {
                if (childNode is XmlElement childElement)
                {
                    stack.AddChild(ParseElement(childElement));
                }
            }
        }
        else if (instance is Border border)
        {
            foreach (XmlNode childNode in element.ChildNodes)
            {
                if (childNode is XmlElement childElement)
                {
                    border.Child = ParseElement(childElement);
                    break; // Border only supports one child
                }
            }
        }
        else if (instance is TuiWindow window)
        {
            foreach (XmlNode childNode in element.ChildNodes)
            {
                if (childNode is XmlElement childElement)
                {
                    window.Content = ParseElement(childElement);
                    break; 
                }
            }
        }

        return instance;
    }

    private static void SetProperty(UIElement instance, string name, string value)
    {
        // Find DependencyProperty field
        var field = instance.GetType().GetField(name + "Property", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (field != null)
        {
            var dp = (DependencyProperty)field.GetValue(null);
            object val = ConvertValue(value, dp.PropertyType);
            instance.SetValue(dp, val);
        }
    }

    private static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType.IsEnum) return Enum.Parse(targetType, value);
        
        return Convert.ChangeType(value, targetType);
    }
}
