using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Microsoft.Win32;
using System.IO;
using IOPath = System.IO.Path;

namespace RDLCReportDesigner
{
    public partial class MainWindow : Window
    {
        private enum DesignTool
        {
            Select,
            TextBox,
            Label,
            Table,
            Image,
            Line,
            Rectangle,
            Chart
        }

        private DesignTool currentTool = DesignTool.Select;
        private bool isDrawing = false;
        private Point startPoint;
        private FrameworkElement selectedElement = null;
        private FrameworkElement dragElement = null;
        private double zoomFactor = 1.0;
        private string currentFileName = null;
        private List<ReportElement> reportElements = new List<ReportElement>();
        private bool isPropertyUpdate = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateStatusText("Report Designer loaded successfully");
        }

        #region Menu Event Handlers

        private void NewReport_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmUnsavedChanges())
            {
                DesignCanvas.Children.Clear();
                reportElements.Clear();
                selectedElement = null;
                UpdatePropertiesPanel();
                currentFileName = null;
                UpdateStatusText("New report created");
            }
        }

        private void OpenReport_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmUnsavedChanges()) return;

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "RDLC Files (*.rdlc)|*.rdlc|All Files (*.*)|*.*",
                DefaultExt = "rdlc"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadReport(dialog.FileName);
                    currentFileName = dialog.FileName;
                    UpdateStatusText($"Report loaded: {IOPath.GetFileName(currentFileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading report: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFileName))
            {
                SaveAsReport_Click(sender, e);
            }
            else
            {
                SaveReport(currentFileName);
            }
        }

        private void SaveAsReport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "RDLC Files (*.rdlc)|*.rdlc|All Files (*.*)|*.*",
                DefaultExt = "rdlc"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveReport(dialog.FileName);
                currentFileName = dialog.FileName;
                UpdateStatusText($"Report saved: {IOPath.GetFileName(currentFileName)}");
            }
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PDF Export functionality would integrate with reporting engine",
                          "Export to PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreviewReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preview functionality would show the rendered report",
                          "Preview Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmUnsavedChanges())
            {
                Close();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("Undo functionality would be implemented here");
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("Redo functionality would be implemented here");
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (selectedElement != null)
            {
                DesignCanvas.Children.Remove(selectedElement);
                var reportElement = reportElements.FirstOrDefault(re => re.UIElement == selectedElement);
                if (reportElement != null)
                {
                    reportElements.Remove(reportElement);
                }
                selectedElement = null;
                UpdatePropertiesPanel();
                UpdateStatusText("Selected element deleted");
            }
        }

        #endregion

        #region Zoom and View Controls

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor = Math.Min(zoomFactor * 1.2, 5.0);
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor = Math.Max(zoomFactor / 1.2, 0.1);
            ApplyZoom();
        }

        private void FitToPage_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            DesignCanvas.RenderTransform = new ScaleTransform(zoomFactor, zoomFactor);
            ZoomLabel.Text = $"{(int)(zoomFactor * 100)}%";
        }

        #endregion

        #region Toolbox Event Handlers

        private void SelectTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Select);
        }

        private void TextBoxTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.TextBox);
        }

        private void LabelTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Label);
        }

        private void TableTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Table);
        }

        private void ImageTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Image);
        }

        private void LineTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Line);
        }

        private void RectangleTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Rectangle);
        }

        private void ChartTool_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DesignTool.Chart);
        }

        private void SetCurrentTool(DesignTool tool)
        {
            currentTool = tool;

            // Reset all button backgrounds
            foreach (Button button in ToolboxPanel.Children.OfType<Button>())
            {
                button.Background = new SolidColorBrush(Colors.LightBlue);
            }

            // Highlight selected tool
            string toolName = tool.ToString();
            var selectedButton = ToolboxPanel.Children.OfType<Button>()
                .FirstOrDefault(b => b.Content.ToString().Replace(" ", "") == toolName ||
                               (toolName == "Select" && b.Name == "SelectButton"));

            if (selectedButton != null)
            {
                selectedButton.Background = new SolidColorBrush(Colors.Yellow);
            }

            UpdateStatusText($"Selected tool: {tool}");
        }

        #endregion

        #region Design Canvas Event Handlers

        private void DesignCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(DesignCanvas);

            if (currentTool == DesignTool.Select)
            {
                HandleSelectTool(clickPoint, e);
            }
            else
            {
                StartDrawing(clickPoint);
            }

            UpdateCoordinateDisplay(clickPoint);
        }

        private void DesignCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(DesignCanvas);
            UpdateCoordinateDisplay(currentPoint);

            if (isDrawing && currentTool != DesignTool.Select)
            {
                UpdateDrawing(currentPoint);
            }
            else if (dragElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                HandleDragMove(currentPoint);
            }
        }

        private void DesignCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                Point endPoint = e.GetPosition(DesignCanvas);
                FinishDrawing(endPoint);
            }

            dragElement = null;
            isDrawing = false;
        }

        private void DesignCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DesignCanvas_Drop(object sender, DragEventArgs e)
        {
            // Handle drag and drop from external sources
            Point dropPoint = e.GetPosition(DesignCanvas);
            UpdateStatusText($"Drop operation at {dropPoint}");
        }

        #endregion

        #region Drawing Operations

        private void StartDrawing(Point startPoint)
        {
            this.startPoint = startPoint;
            isDrawing = true;
            DesignCanvas.CaptureMouse();
        }

        private void UpdateDrawing(Point currentPoint)
        {
            // Update preview of element being drawn
            // Implementation would depend on the specific tool
        }

        private void FinishDrawing(Point endPoint)
        {
            DesignCanvas.ReleaseMouseCapture();

            double left = Math.Min(startPoint.X, endPoint.X);
            double top = Math.Min(startPoint.Y, endPoint.Y);
            double width = Math.Abs(endPoint.X - startPoint.X);
            double height = Math.Abs(endPoint.Y - startPoint.Y);

            // Minimum size constraint
            width = Math.Max(width, 20);
            height = Math.Max(height, 20);

            FrameworkElement newElement = CreateElement(currentTool, left, top, width, height);
            if (newElement != null)
            {
                DesignCanvas.Children.Add(newElement);

                // Create report element
                var reportElement = new ReportElement
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = currentTool.ToString(),
                    UIElement = newElement,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height
                };
                reportElements.Add(reportElement);

                // Select the new element
                SelectElement(newElement);

                UpdateStatusText($"Created {currentTool} at ({left:F0}, {top:F0})");
            }

            isDrawing = false;
        }

        private FrameworkElement CreateElement(DesignTool tool, double left, double top, double width, double height)
        {
            FrameworkElement element = null;

            switch (tool)
            {
                case DesignTool.TextBox:
                    var textBox = new TextBox
                    {
                        Text = "TextBox",
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White
                    };
                    element = textBox;
                    break;

                case DesignTool.Label:
                    var label = new Label
                    {
                        Content = "Label",
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.Transparent
                    };
                    element = label;
                    break;

                case DesignTool.Rectangle:
                    var rectangle = new Rectangle
                    {
                        Fill = Brushes.LightGray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    element = rectangle;
                    break;

                case DesignTool.Line:
                    var line = new Line
                    {
                        X1 = 0,
                        Y1 = 0,
                        X2 = width,
                        Y2 = height,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    element = line;
                    break;

                case DesignTool.Table:
                    var border = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White
                    };
                    var textBlock = new TextBlock
                    {
                        Text = "Table",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = textBlock;
                    element = border;
                    break;

                case DesignTool.Image:
                    var imageBorder = new Border
                    {
                        BorderBrush = Brushes.DarkGray,
                        BorderThickness = new Thickness(2),
                        Background = Brushes.LightGray
                    };
                    var imageText = new TextBlock
                    {
                        Text = "Image",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontStyle = FontStyles.Italic
                    };
                    imageBorder.Child = imageText;
                    element = imageBorder;
                    break;

                case DesignTool.Chart:
                    var chartBorder = new Border
                    {
                        BorderBrush = Brushes.Blue,
                        BorderThickness = new Thickness(2),
                        Background = Brushes.AliceBlue
                    };
                    var chartText = new TextBlock
                    {
                        Text = "Chart",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold
                    };
                    chartBorder.Child = chartText;
                    element = chartBorder;
                    break;
            }

            if (element != null)
            {
                Canvas.SetLeft(element, left);
                Canvas.SetTop(element, top);
                element.Width = width;
                element.Height = height;
                element.Cursor = Cursors.SizeAll;

                // Add event handlers for selection and dragging
                element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            }

            return element;
        }

        #endregion

        #region Element Selection and Manipulation

        private void HandleSelectTool(Point clickPoint, MouseButtonEventArgs e)
        {
            var hitElement = GetElementAtPoint(clickPoint);

            if (hitElement != null)
            {
                SelectElement(hitElement);
                startPoint = clickPoint;
                dragElement = hitElement;
                e.Handled = true;
            }
            else
            {
                SelectElement(null);
            }
        }

        private void SelectElement(FrameworkElement element)
        {
            // Remove previous selection highlight
            if (selectedElement != null)
            {
                RemoveSelectionHighlight(selectedElement);
            }

            selectedElement = element;

            if (selectedElement != null)
            {
                AddSelectionHighlight(selectedElement);
            }

            UpdatePropertiesPanel();
        }

        private void AddSelectionHighlight(FrameworkElement element)
        {
            // Add visual selection indicator
            var adorner = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(adorner, Canvas.GetLeft(element) - 2);
            Canvas.SetTop(adorner, Canvas.GetTop(element) - 2);
            adorner.Width = element.Width + 4;
            adorner.Height = element.Height + 4;
            adorner.Tag = "SelectionAdorner";

            DesignCanvas.Children.Add(adorner);
        }

        private void RemoveSelectionHighlight(FrameworkElement element)
        {
            var adorners = DesignCanvas.Children.OfType<Rectangle>()
                .Where(r => r.Tag?.ToString() == "SelectionAdorner")
                .ToList();

            foreach (var adorner in adorners)
            {
                DesignCanvas.Children.Remove(adorner);
            }
        }

        private FrameworkElement GetElementAtPoint(Point point)
        {
            return DesignCanvas.Children.OfType<FrameworkElement>()
                .Where(element => element.Tag?.ToString() != "SelectionAdorner")
                .LastOrDefault(element =>
                {
                    double left = Canvas.GetLeft(element);
                    double top = Canvas.GetTop(element);
                    return point.X >= left && point.X <= left + element.Width &&
                           point.Y >= top && point.Y <= top + element.Height;
                });
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null && currentTool == DesignTool.Select)
            {
                SelectElement(element);
                startPoint = e.GetPosition(DesignCanvas);
                dragElement = element;
                e.Handled = true;
            }
        }

        private void HandleDragMove(Point currentPoint)
        {
            if (dragElement != null)
            {
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;

                double newLeft = Canvas.GetLeft(dragElement) + deltaX;
                double newTop = Canvas.GetTop(dragElement) + deltaY;

                // Constrain to canvas bounds
                newLeft = Math.Max(0, Math.Min(newLeft, DesignCanvas.Width - dragElement.Width));
                newTop = Math.Max(0, Math.Min(newTop, DesignCanvas.Height - dragElement.Height));

                Canvas.SetLeft(dragElement, newLeft);
                Canvas.SetTop(dragElement, newTop);

                // Update selection highlight
                RemoveSelectionHighlight(dragElement);
                AddSelectionHighlight(dragElement);

                // Update report element
                var reportElement = reportElements.FirstOrDefault(re => re.UIElement == dragElement);
                if (reportElement != null)
                {
                    reportElement.X = newLeft;
                    reportElement.Y = newTop;
                }

                UpdatePropertiesPanel();
                startPoint = currentPoint;
            }
        }

        #endregion

        #region Properties Panel

        private void UpdatePropertiesPanel()
        {
            if (selectedElement == null)
            {
                NoSelectionText.Visibility = Visibility.Visible;
                PropertyControls.Visibility = Visibility.Collapsed;
                return;
            }

            NoSelectionText.Visibility = Visibility.Collapsed;
            PropertyControls.Visibility = Visibility.Visible;

            isPropertyUpdate = true;

            // Update property values
            var reportElement = reportElements.FirstOrDefault(re => re.UIElement == selectedElement);
            if (reportElement != null)
            {
                NameTextBox.Text = reportElement.Id;
                XTextBox.Text = Math.Round(Canvas.GetLeft(selectedElement)).ToString();
                YTextBox.Text = Math.Round(Canvas.GetTop(selectedElement)).ToString();
                WidthTextBox.Text = Math.Round(selectedElement.Width).ToString();
                HeightTextBox.Text = Math.Round(selectedElement.Height).ToString();

                // Element-specific properties
                if (selectedElement is TextBox textBox)
                {
                    TextTextBox.Text = textBox.Text;
                    FontSizeTextBox.Text = textBox.FontSize.ToString();
                }
                else if (selectedElement is Label label)
                {
                    TextTextBox.Text = label.Content?.ToString() ?? "";
                    FontSizeTextBox.Text = label.FontSize.ToString();
                }
                else
                {
                    TextTextBox.Text = "";
                    FontSizeTextBox.Text = "";
                }

                // Background color
                var background = selectedElement.GetValue(Control.BackgroundProperty) as SolidColorBrush;
                if (background != null)
                {
                    SetComboBoxSelection(BackgroundColorCombo, GetColorName(background.Color));
                }
            }

            isPropertyUpdate = false;
        }

        private void Property_Changed(object sender, EventArgs e)
        {
            if (isPropertyUpdate || selectedElement == null) return;

            try
            {
                // Update position and size
                if (double.TryParse(XTextBox.Text, out double x))
                {
                    Canvas.SetLeft(selectedElement, x);
                }

                if (double.TryParse(YTextBox.Text, out double y))
                {
                    Canvas.SetTop(selectedElement, y);
                }

                if (double.TryParse(WidthTextBox.Text, out double width) && width > 0)
                {
                    selectedElement.Width = width;
                }

                if (double.TryParse(HeightTextBox.Text, out double height) && height > 0)
                {
                    selectedElement.Height = height;
                }

                // Update text content
                if (selectedElement is TextBox textBox && !string.IsNullOrEmpty(TextTextBox.Text))
                {
                    textBox.Text = TextTextBox.Text;
                }
                else if (selectedElement is Label label && !string.IsNullOrEmpty(TextTextBox.Text))
                {
                    label.Content = TextTextBox.Text;
                }

                // Update font size
                if (double.TryParse(FontSizeTextBox.Text, out double fontSize) && fontSize > 0)
                {
                    if (selectedElement is Control control)
                    {
                        control.FontSize = fontSize;
                    }
                }

                // Update background color
                if (BackgroundColorCombo.SelectedItem is ComboBoxItem selectedColor)
                {
                    var brush = GetBrushFromColorName(selectedColor.Content.ToString());
                    if (selectedElement is Control ctrl)
                    {
                        ctrl.Background = brush;
                    }
                    else if (selectedElement is Shape shape)
                    {
                        shape.Fill = brush;
                    }
                }

                // Update report element data
                var reportElement = reportElements.FirstOrDefault(re => re.UIElement == selectedElement);
                if (reportElement != null)
                {
                    reportElement.X = Canvas.GetLeft(selectedElement);
                    reportElement.Y = Canvas.GetTop(selectedElement);
                    reportElement.Width = selectedElement.Width;
                    reportElement.Height = selectedElement.Height;
                }

                // Update selection highlight
                RemoveSelectionHighlight(selectedElement);
                AddSelectionHighlight(selectedElement);

                UpdateStatusText("Properties updated");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"Error updating properties: {ex.Message}");
            }
        }

        private string GetColorName(Color color)
        {
            if (color == Colors.Transparent) return "Transparent";
            if (color == Colors.White) return "White";
            if (color == Colors.LightGray) return "LightGray";
            if (color == Colors.Yellow) return "Yellow";
            if (color == Colors.LightBlue) return "LightBlue";
            return "White";
        }

        private Brush GetBrushFromColorName(string colorName)
        {
            return colorName switch
            {
                "Transparent" => Brushes.Transparent,
                "White" => Brushes.White,
                "LightGray" => Brushes.LightGray,
                "Yellow" => Brushes.Yellow,
                "LightBlue" => Brushes.LightBlue,
                _ => Brushes.White
            };
        }

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            var item = comboBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == value);
            if (item != null)
            {
                comboBox.SelectedItem = item;
            }
        }

        #endregion

        #region File Operations

        private bool ConfirmUnsavedChanges()
        {
            // In a real implementation, check if there are unsaved changes
            return true;
        }

        private void LoadReport(string fileName)
        {
            // Clear existing content
            DesignCanvas.Children.Clear();
            reportElements.Clear();

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"File not found: {fileName}");
            }

            try
            {
                // Load RDLC content
                var doc = XDocument.Load(fileName);
                var reportItems = doc.Descendants()
                    .Where(e => e.Name.LocalName == "Textbox" ||
                               e.Name.LocalName == "Rectangle" ||
                               e.Name.LocalName == "Line" ||
                               e.Name.LocalName == "Image" ||
                               e.Name.LocalName == "Tablix")
                    .ToList();

                foreach (var item in reportItems)
                {
                    LoadReportItem(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing RDLC file: {ex.Message}");
            }
        }

        private void LoadReportItem(XElement item)
        {
            try
            {
                string name = item.Attribute("Name")?.Value ?? "UnnamedItem";
                string itemType = item.Name.LocalName;

                // Parse position and size
                var left = ParseMeasurement(item.Element(item.Name.Namespace + "Left")?.Value ?? "0in");
                var top = ParseMeasurement(item.Element(item.Name.Namespace + "Top")?.Value ?? "0in");
                var width = ParseMeasurement(item.Element(item.Name.Namespace + "Width")?.Value ?? "1in");
                var height = ParseMeasurement(item.Element(item.Name.Namespace + "Height")?.Value ?? "0.25in");

                // Create appropriate UI element
                FrameworkElement element = null;
                DesignTool toolType = DesignTool.Label;

                switch (itemType.ToLower())
                {
                    case "textbox":
                        var textValue = item.Element(item.Name.Namespace + "Paragraphs")
                            ?.Element(item.Name.Namespace + "Paragraph")
                            ?.Element(item.Name.Namespace + "TextRuns")
                            ?.Element(item.Name.Namespace + "TextRun")
                            ?.Element(item.Name.Namespace + "Value")?.Value ?? "TextBox";

                        element = new TextBox
                        {
                            Text = textValue,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            Background = Brushes.White
                        };
                        toolType = DesignTool.TextBox;
                        break;

                    case "rectangle":
                        element = new Rectangle
                        {
                            Fill = Brushes.LightGray,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        toolType = DesignTool.Rectangle;
                        break;

                    case "line":
                        element = new Line
                        {
                            X1 = 0,
                            Y1 = 0,
                            X2 = width,
                            Y2 = height,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        toolType = DesignTool.Line;
                        break;

                    case "image":
                        var imageBorder = new Border
                        {
                            BorderBrush = Brushes.DarkGray,
                            BorderThickness = new Thickness(2),
                            Background = Brushes.LightGray
                        };
                        imageBorder.Child = new TextBlock
                        {
                            Text = "Image",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        element = imageBorder;
                        toolType = DesignTool.Image;
                        break;

                    case "tablix":
                        var tableBorder = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            Background = Brushes.White
                        };
                        tableBorder.Child = new TextBlock
                        {
                            Text = "Table",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        element = tableBorder;
                        toolType = DesignTool.Table;
                        break;
                }

                if (element != null)
                {
                    Canvas.SetLeft(element, left);
                    Canvas.SetTop(element, top);
                    element.Width = width;
                    element.Height = height;
                    element.Cursor = Cursors.SizeAll;
                    element.MouseLeftButtonDown += Element_MouseLeftButtonDown;

                    DesignCanvas.Children.Add(element);

                    var reportElement = new ReportElement
                    {
                        Id = name,
                        Type = toolType.ToString(),
                        UIElement = element,
                        X = left,
                        Y = top,
                        Width = width,
                        Height = height
                    };
                    reportElements.Add(reportElement);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"Warning: Could not load item {item.Name.LocalName}: {ex.Message}");
            }
        }

        private double ParseMeasurement(string measurement)
        {
            if (string.IsNullOrEmpty(measurement)) return 0;

            // Convert common RDLC measurements to pixels (96 DPI)
            measurement = measurement.Trim().ToLower();

            if (measurement.EndsWith("in"))
            {
                if (double.TryParse(measurement.Substring(0, measurement.Length - 2), out double inches))
                    return inches * 96; // 96 pixels per inch
            }
            else if (measurement.EndsWith("cm"))
            {
                if (double.TryParse(measurement.Substring(0, measurement.Length - 2), out double cm))
                    return cm * 37.8; // Approximate pixels per cm
            }
            else if (measurement.EndsWith("pt"))
            {
                if (double.TryParse(measurement.Substring(0, measurement.Length - 2), out double points))
                    return points * 1.33; // Approximate pixels per point
            }
            else if (measurement.EndsWith("px"))
            {
                if (double.TryParse(measurement.Substring(0, measurement.Length - 2), out double pixels))
                    return pixels;
            }
            else
            {
                // Try to parse as raw number (assume pixels)
                if (double.TryParse(measurement, out double value))
                    return value;
            }

            return 0;
        }

        private void SaveReport(string fileName)
        {
            try
            {
                var rdlc = GenerateRDLC();
                File.WriteAllText(fileName, rdlc);
                UpdateStatusText($"Report saved successfully to {IOPath.GetFileName(fileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving report: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateRDLC()
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");

            var report = new XElement(ns + "Report",
                new XAttribute("xmlns", ns.NamespaceName),
                new XElement(ns + "Body",
                    new XElement(ns + "ReportItems",
                        reportElements.Select(CreateRDLCElement).ToArray()
                    ),
                    new XElement(ns + "Height", "11in"),
                    new XElement(ns + "Style")
                ),
                new XElement(ns + "Width", "8.5in"),
                new XElement(ns + "Page",
                    new XElement(ns + "PageHeight", "11in"),
                    new XElement(ns + "PageWidth", "8.5in"),
                    new XElement(ns + "LeftMargin", "1in"),
                    new XElement(ns + "RightMargin", "1in"),
                    new XElement(ns + "TopMargin", "1in"),
                    new XElement(ns + "BottomMargin", "1in"),
                    new XElement(ns + "Style")
                )
            );

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                report
            );

            return doc.ToString();
        }

        private XElement CreateRDLCElement(ReportElement element)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");

            var leftInches = (element.X / 96.0).ToString("F2") + "in";
            var topInches = (element.Y / 96.0).ToString("F2") + "in";
            var widthInches = (element.Width / 96.0).ToString("F2") + "in";
            var heightInches = (element.Height / 96.0).ToString("F2") + "in";

            switch (element.Type.ToLower())
            {
                case "textbox":
                    var textContent = "";
                    if (element.UIElement is TextBox tb)
                        textContent = tb.Text;

                    return new XElement(ns + "Textbox",
                        new XAttribute("Name", element.Id),
                        new XElement(ns + "CanGrow", "true"),
                        new XElement(ns + "KeepTogether", "true"),
                        new XElement(ns + "Paragraphs",
                            new XElement(ns + "Paragraph",
                                new XElement(ns + "TextRuns",
                                    new XElement(ns + "TextRun",
                                        new XElement(ns + "Value", textContent),
                                        new XElement(ns + "Style")
                                    )
                                ),
                                new XElement(ns + "Style")
                            )
                        ),
                        new XElement(ns + "rd:DefaultName", element.Id),
                        new XElement(ns + "Top", topInches),
                        new XElement(ns + "Left", leftInches),
                        new XElement(ns + "Height", heightInches),
                        new XElement(ns + "Width", widthInches),
                        new XElement(ns + "Style",
                            new XElement(ns + "Border",
                                new XElement(ns + "Style", "None")
                            ),
                            new XElement(ns + "PaddingLeft", "2pt"),
                            new XElement(ns + "PaddingRight", "2pt"),
                            new XElement(ns + "PaddingTop", "2pt"),
                            new XElement(ns + "PaddingBottom", "2pt")
                        )
                    );

                case "rectangle":
                    return new XElement(ns + "Rectangle",
                        new XAttribute("Name", element.Id),
                        new XElement(ns + "Top", topInches),
                        new XElement(ns + "Left", leftInches),
                        new XElement(ns + "Height", heightInches),
                        new XElement(ns + "Width", widthInches),
                        new XElement(ns + "ZIndex", "1"),
                        new XElement(ns + "Style",
                            new XElement(ns + "Border",
                                new XElement(ns + "Style", "Solid")
                            )
                        )
                    );

                case "line":
                    return new XElement(ns + "Line",
                        new XAttribute("Name", element.Id),
                        new XElement(ns + "Top", topInches),
                        new XElement(ns + "Left", leftInches),
                        new XElement(ns + "Height", heightInches),
                        new XElement(ns + "Width", widthInches),
                        new XElement(ns + "ZIndex", "2"),
                        new XElement(ns + "Style",
                            new XElement(ns + "Border",
                                new XElement(ns + "Style", "Solid")
                            )
                        )
                    );

                default:
                    // Default to textbox for unknown types
                    return new XElement(ns + "Textbox",
                        new XAttribute("Name", element.Id),
                        new XElement(ns + "CanGrow", "true"),
                        new XElement(ns + "KeepTogether", "true"),
                        new XElement(ns + "Paragraphs",
                            new XElement(ns + "Paragraph",
                                new XElement(ns + "TextRuns",
                                    new XElement(ns + "TextRun",
                                        new XElement(ns + "Value", element.Type),
                                        new XElement(ns + "Style")
                                    )
                                ),
                                new XElement(ns + "Style")
                            )
                        ),
                        new XElement(ns + "rd:DefaultName", element.Id),
                        new XElement(ns + "Top", topInches),
                        new XElement(ns + "Left", leftInches),
                        new XElement(ns + "Height", heightInches),
                        new XElement(ns + "Width", widthInches),
                        new XElement(ns + "Style")
                    );
            }
        }

        #endregion

        #region Utility Methods

        private void UpdateStatusText(string text)
        {
            StatusText.Text = text;
        }

        private void UpdateCoordinateDisplay(Point point)
        {
            CoordinateText.Text = $"X: {point.X:F0}, Y: {point.Y:F0}";
        }

        #endregion
    }

    // Helper class to represent report elements
    public class ReportElement
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public FrameworkElement UIElement { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}