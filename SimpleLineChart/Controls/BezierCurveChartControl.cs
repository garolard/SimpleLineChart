using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace SimpleLineChart.Controls
{
    [TemplatePart(Name = PART_VIEWBOX, Type = typeof(Viewbox))]
    [TemplatePart(Name = PART_CANVAS, Type = typeof(Canvas))]
    public class BezierCurveChartControl : Control
    {
        private const string PART_VIEWBOX = "PartViewbox";
        private const string PART_CANVAS = "PartCanvas";


        private Viewbox _viewboxPart;
        private Canvas _canvasPart;
        

        private double _normalizedItemWidth;
        private Point _chartStartPoint;
        private IList<Shape> _pointsEllipses;
        private PathSegmentCollection _segmentCollection;
        

        public static readonly DependencyProperty PathWidthProperty = DependencyProperty.Register(
            "PathWidth", typeof(int), typeof(BezierCurveChartControl), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty PathHeightProperty = DependencyProperty.Register(
            "PathHeight", typeof(int), typeof(BezierCurveChartControl), new PropertyMetadata(default(int)));

        //public static readonly DependencyProperty DesiredItemWidthProperty = DependencyProperty.Register(
        //    "DesiredItemWidth", typeof(int), typeof(BezierCurveChartControl), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty MaxValueYAxisProperty = DependencyProperty.Register(
            "MaxValueYAxis", typeof(double), typeof(BezierCurveChartControl), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty DrawValuePointsProperty = DependencyProperty.Register(
            "DrawValuePoints", typeof(bool), typeof(BezierCurveChartControl), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data", typeof(ICollection<double>), typeof(BezierCurveChartControl), new PropertyMetadata(default(ICollection<double>), OnDataChanged));

        public static readonly DependencyProperty ValuePointDecoratorProperty = DependencyProperty.Register(
            "ValuePointDecorator", typeof(DataTemplate), typeof(BezierCurveChartControl), new PropertyMetadata(default(DataTemplate)));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(BezierCurveChartControl), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", typeof(double), typeof(BezierCurveChartControl), new PropertyMetadata(default(double)));


        public BezierCurveChartControl()
        {
            this.DefaultStyleKey = typeof(BezierCurveChartControl);
        }


        protected override void OnApplyTemplate()
        {
            _viewboxPart = (Viewbox) GetTemplateChild(PART_VIEWBOX);
            _canvasPart = (Canvas) GetTemplateChild(PART_CANVAS);

            _canvasPart.Width = PathWidth;
            _viewboxPart.MaxWidth = PathWidth;
            _canvasPart.Height = PathHeight;
            _viewboxPart.MaxHeight = PathHeight;

            DrawChart();

            base.OnApplyTemplate();
        }


        public int PathWidth
        {
            get { return (int)GetValue(PathWidthProperty); }
            set { SetValue(PathWidthProperty, value); }
        }

        public int PathHeight
        {
            get { return (int)GetValue(PathHeightProperty); }
            set { SetValue(PathHeightProperty, value); }
        }

        //public int DesiredItemWidth
        //{
        //    get { return (int)GetValue(DesiredItemWidthProperty); }
        //    set { SetValue(DesiredItemWidthProperty, value); }
        //}

        public double MaxValueYAxis
        {
            get { return (double)GetValue(MaxValueYAxisProperty); }
            set { SetValue(MaxValueYAxisProperty, value); }
        }

        public bool DrawValuePoints
        {
            get { return (bool)GetValue(DrawValuePointsProperty); }
            set { SetValue(DrawValuePointsProperty, value); }
        }

        public ICollection<double> Data
        {
            get { return (ICollection<double>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public DataTemplate ValuePointDecorator
        {
            get { return (DataTemplate)GetValue(ValuePointDecoratorProperty); }
            set { SetValue(ValuePointDecoratorProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }


        public void DrawChart()
        {
            if (_canvasPart == null) return;

            _normalizedItemWidth = (double)PathWidth / Data.Count;
            if (MaxValueYAxis == 0)
            {
                if (!Data.Any()) MaxValueYAxis = 0;
                else MaxValueYAxis = Data.Max() + 20;
            }

            SetChartStartPoint();
            CreateChartSegments();
            if (DrawValuePoints) CreatePointsGeometries();

            var path = GetPathFromSegments();

            _canvasPart.Children.Add(path);
            if (DrawValuePoints)
            {
                foreach (var ellipse in _pointsEllipses)
                {
                    _canvasPart.Children.Add(ellipse);
                }
            }
        }

        private void SetChartStartPoint()
        {
            var firstValue = Data.FirstOrDefault();
            _chartStartPoint = GetNormalizedPoint(0, firstValue);
        }

        private void CreateChartSegments()
        {
            _segmentCollection = new PathSegmentCollection();
            var values = new List<double>(Data);
            for (var i = 0; i < values.Count - 1; i++)
            {
                var segmentStartPoint = GetNormalizedPoint(i, values[i]);
                var segmentEndPoint = GetNormalizedPoint(i + 1, values[i + 1]);

                var segmentFirstControlPoint = GetControlPoint(segmentStartPoint, 1);
                var segmentSecondControlPoint = GetControlPoint(segmentEndPoint, -1);

                var segment = new BezierSegment()
                {
                    Point1 = segmentFirstControlPoint,
                    Point2 = segmentSecondControlPoint,
                    Point3 = segmentEndPoint
                };
                _segmentCollection.Add(segment);
            }
        }

        private Point GetNormalizedPoint(int pointPosition, double actualValue)
        {
            var x = (_normalizedItemWidth / 2) + _normalizedItemWidth * pointPosition;
            var y = PathHeight - (actualValue * PathHeight) / MaxValueYAxis;
            return new Point(x, y);
        }

        private Point GetControlPoint(Point segmentStartPoint, int direction)
        {
            double x = 0.0;

            if (direction > 0)
                x = segmentStartPoint.X + 28;
            else
                x = segmentStartPoint.X - 28;

            var y = segmentStartPoint.Y;
            return new Point(x, y);
        }

        private void CreatePointsGeometries()
        {
            _pointsEllipses = new List<Shape>();
            var values = new List<double>(Data);
            for (var i = 0; i < values.Count; i++)
            {
                var point = GetNormalizedPoint(i, values[i]);
                Shape pointShape = null;

                if (ValuePointDecorator != null)
                {
                    pointShape = ValuePointDecorator.LoadContent() as Shape;
                }
                else
                {
                    pointShape = new Ellipse()
                    {
                        Fill = new SolidColorBrush(Colors.Black),
                        Width = 4,
                        Height = 4
                    };
                }

                var shapeX = double.IsNaN(pointShape.Width)
                    ? point.X
                    : point.X - pointShape.Width / 2;
                var shapeY = double.IsNaN(pointShape.Height)
                    ? point.Y
                    : point.Y - pointShape.Height / 2;

                Canvas.SetLeft(pointShape, shapeX);
                Canvas.SetTop(pointShape, shapeY);
                _pointsEllipses.Add(pointShape);
            }
        }

        private Path GetPathFromSegments()
        {
            var startFigure = new PathFigure
            {
                StartPoint = _chartStartPoint,
                Segments = _segmentCollection
            };

            var pathGeometry = new PathGeometry
            {
                Figures = new PathFigureCollection { startFigure }
            };

            var pathStroke = Stroke ?? new SolidColorBrush(Colors.Black);
            var pathStrokeThickness = double.IsNaN(StrokeThickness) || StrokeThickness == 0
                ? 1.0
                : StrokeThickness;

            var path = new Path()
            {
                Stroke = pathStroke,
                StrokeThickness = pathStrokeThickness,
                Data = pathGeometry
            };
            return path;
        }


        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as BezierCurveChartControl;
            chart?.DrawChart();
        }
    }
}