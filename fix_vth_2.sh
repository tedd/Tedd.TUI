#!/bin/bash
sed -i 's/System.Windows.Media.VisualTreeHelper.GetDpi/System.Windows.Media.VisualTreeHelper.GetDpi/g' src/Tedd.TUI.Platform.Wpf/TuiHostElement.cs
