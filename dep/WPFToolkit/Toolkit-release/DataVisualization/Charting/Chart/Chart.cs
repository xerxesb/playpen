﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Controls.DataVisualization.Charting.Primitives;

namespace System.Windows.Controls.DataVisualization.Charting
{
    /// <summary>
    /// Represents a control that displays a Chart.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    [TemplatePart(Name = Chart.ChartAreaName, Type = typeof(EdgePanel))]
    [TemplatePart(Name = Chart.LegendName, Type = typeof(Legend))]
    [StyleTypedProperty(Property = "TitleStyle", StyleTargetType = typeof(Title))]
    [StyleTypedProperty(Property = "LegendStyle", StyleTargetType = typeof(Legend))]
    [StyleTypedProperty(Property = "ChartAreaStyle", StyleTargetType = typeof(EdgePanel))]
    [StyleTypedProperty(Property = "PlotAreaStyle", StyleTargetType = typeof(Grid))]
    [ContentProperty("Series")]
    public sealed partial class Chart : Control, ISeriesHost
    {
        /// <summary>
        /// Specifies the name of the ChartArea TemplatePart.
        /// </summary>
        private const string ChartAreaName = "ChartArea";

        /// <summary>
        /// Specifies the name of the legend TemplatePart.
        /// </summary>
        private const string LegendName = "Legend";

        /// <summary>
        /// Stores the legend children.
        /// </summary>
        private ObservableCollectionListAdapter<UIElement> _legendChildrenLegendAdapter = new ObservableCollectionListAdapter<UIElement>();

        /// <summary>
        /// Gets or sets the chart area children collection.
        /// </summary>
        private AggregatedObservableCollection<UIElement> ChartAreaChildren { get; set; }

        /// <summary>
        /// An adapter that synchronizes changes to the ChartAreaChildren
        /// property to the ChartArea panel's children collection.
        /// </summary>
        private ObservableCollectionListAdapter<UIElement> _chartAreaChildrenListAdapter = new ObservableCollectionListAdapter<UIElement>();

        /// <summary>
        /// Gets or sets a collection of Axes in the Chart.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Setter is public to work around a limitation with the XAML editing tools.")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Setter is public to work around a limitation with the XAML editing tools.")]
        public Collection<IAxis> Axes
        {
            get
            {
                return _axes;
            }
            set
            {
                throw new NotSupportedException(Properties.Resources.Chart_Axes_SetterNotSupported);
            }
        }

        /// <summary>
        /// Stores the collection of Axes in the Chart.
        /// </summary>
        private Collection<IAxis> _axes;

        /// <summary>
        /// The collection of foreground elements.
        /// </summary>
        private ObservableCollection<UIElement> _foregroundElements = new NoResetObservableCollection<UIElement>();

        /// <summary>
        /// The collection of background elements.
        /// </summary>
        private ObservableCollection<UIElement> _backgroundElements = new NoResetObservableCollection<UIElement>();

        /// <summary>
        /// Gets the collection of foreground elements.
        /// </summary>
        ObservableCollection<UIElement> ISeriesHost.ForegroundElements { get { return _foregroundElements; } }

        /// <summary>
        /// Gets the collection of background elements.
        /// </summary>
        ObservableCollection<UIElement> ISeriesHost.BackgroundElements { get { return _backgroundElements; } }

        /// <summary>
        /// Axes arranged along the edges.
        /// </summary>
        private ObservableCollection<Axis> _edgeAxes = new NoResetObservableCollection<Axis>();

        /// <summary>
        /// Gets or sets the axes that are currently in the chart.
        /// </summary>
        private IList<IAxis> InternalActualAxes { get; set; }

        /// <summary>
        /// Gets the actual axes displayed in the chart.
        /// </summary>
        public ReadOnlyCollection<IAxis> ActualAxes { get; private set; }

        /// <summary>
        /// Gets or sets the reference to the template's ChartArea.
        /// </summary>
        private EdgePanel ChartArea { get; set; }

        /// <summary>
        /// Gets or sets the reference to the Chart's Legend.
        /// </summary>
        private Legend Legend { get; set; }

        /// <summary>
        /// Gets or sets the collection of Series displayed by the Chart.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Setter is public to work around a limitation with the XAML editing tools.")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Setter is public to work around a limitation with the XAML editing tools.")]
        public Collection<Series> Series
        {
            get
            {
                return _series;
            }
            set
            {
                throw new NotSupportedException(Properties.Resources.Chart_Series_SetterNotSupported);
            }
        }

        /// <summary>
        /// Stores the collection of Series displayed by the Chart.
        /// </summary>
        private Collection<Series> _series;

        #region public Style ChartAreaStyle
        /// <summary>
        /// Gets or sets the Style of the ISeriesHost's ChartArea.
        /// </summary>
        public Style ChartAreaStyle
        {
            get { return GetValue(ChartAreaStyleProperty) as Style; }
            set { SetValue(ChartAreaStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the ChartAreaStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty ChartAreaStyleProperty =
            DependencyProperty.Register(
                "ChartAreaStyle",
                typeof(Style),
                typeof(Chart),
                null);
        #endregion public Style ChartAreaStyle

        /// <summary>
        /// Gets the collection of legend items.
        /// </summary>
        public IList LegendItems { get; private set; }

        #region public Style LegendStyle
        /// <summary>
        /// Gets or sets the Style of the ISeriesHost's Legend.
        /// </summary>
        public Style LegendStyle
        {
            get { return GetValue(LegendStyleProperty) as Style; }
            set { SetValue(LegendStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the LegendStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendStyleProperty =
            DependencyProperty.Register(
                "LegendStyle",
                typeof(Style),
                typeof(Chart),
                null);
        #endregion public Style LegendStyle

        #region public object LegendTitle
        /// <summary>
        /// Gets or sets the Title content of the Legend.
        /// </summary>
        public object LegendTitle
        {
            get { return GetValue(LegendTitleProperty); }
            set { SetValue(LegendTitleProperty, value); }
        }

        /// <summary>
        /// Identifies the LegendTitle dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendTitleProperty =
            DependencyProperty.Register(
                "LegendTitle",
                typeof(object),
                typeof(Chart),
                null);
        #endregion public object LegendTitle

        #region public Style PlotAreaStyle
        /// <summary>
        /// Gets or sets the Style of the ISeriesHost's PlotArea.
        /// </summary>
        public Style PlotAreaStyle
        {
            get { return GetValue(PlotAreaStyleProperty) as Style; }
            set { SetValue(PlotAreaStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the PlotAreaStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotAreaStyleProperty =
            DependencyProperty.Register(
                "PlotAreaStyle",
                typeof(Style),
                typeof(Chart),
                null);
        #endregion public Style PlotAreaStyle

        #region public Collection<Style> StylePalette
        /// <summary>
        /// Gets or sets a palette of styles used by the children of the ISeriesHost.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Want to allow this to be set from XAML.")]
        public Collection<Style> StylePalette
        {
            get { return GetValue(StylePaletteProperty) as Collection<Style>; }
            set { SetValue(StylePaletteProperty, value); }
        }

        /// <summary>
        /// Identifies the StylePalette dependency property.
        /// </summary>
        public static readonly DependencyProperty StylePaletteProperty =
            DependencyProperty.Register(
                "StylePalette",
                typeof(Collection<Style>),
                typeof(Chart),
                new PropertyMetadata(OnStylePalettePropertyChanged));

        /// <summary>
        /// Called when the value of the StylePalette property is changed.
        /// </summary>
        /// <param name="d">Chart that contains the changed StylePalette.
        /// </param>
        /// <param name="e">Event arguments.</param>
        private static void OnStylePalettePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Chart source = (Chart) d;
            Collection<Style> newValue = (Collection<Style>) e.NewValue;
            source.OnStylePalettePropertyChanged(newValue);
        }

        /// <summary>
        /// Called when the value of the StylePalette property is changed.
        /// </summary>
        /// <param name="newValue">The new value for the StylePalette.</param>
        private void OnStylePalettePropertyChanged(Collection<Style> newValue)
        {
            StyleDispenser.Styles = newValue;
            foreach (Series series in this.Series)
            {
                series.RefreshStyles();
            }
        }
        #endregion public Collection<Style> StylePalette

        /// <summary>
        /// Gets or sets an object that rotates through the palette.
        /// </summary>
        private StyleDispenser StyleDispenser { get; set; }

        #region public object Title
        /// <summary>
        /// Gets or sets the title displayed for the Chart.
        /// </summary>
        public object Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Identifies the Title dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(object),
                typeof(Chart),
                null);
        #endregion

        #region public Style TitleStyle
        /// <summary>
        /// Gets or sets the Style of the ISeriesHost's Title.
        /// </summary>
        public Style TitleStyle
        {
            get { return GetValue(TitleStyleProperty) as Style; }
            set { SetValue(TitleStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the TitleStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleStyleProperty =
            DependencyProperty.Register(
                "TitleStyle",
                typeof(Style),
                typeof(Chart),
                null);
        #endregion public Style TitleStyle

#if !SILVERLIGHT
        /// <summary>
        /// Initializes the static members of the Chart class.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Dependency properties are initialized in-line.")]
        static Chart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Chart), new FrameworkPropertyMetadata(typeof(Chart)));
        }

#endif
        /// <summary>
        /// Initializes a new instance of the Chart class.
        /// </summary>
        public Chart()
        {
#if SILVERLIGHT
            DefaultStyleKey = typeof(Chart);
#endif
            // Create the backing collection for Series
            UniqueObservableCollection<Series> series = new UniqueObservableCollection<Series>();
            series.CollectionChanged += new NotifyCollectionChangedEventHandler(OnSeriesCollectionChanged);
            _series = series;

            // Create the backing collection for Axes
            UniqueObservableCollection<IAxis> axes = new UniqueObservableCollection<IAxis>();
            _axes = axes;

            ObservableCollection<IAxis> actualAxes = new SeriesHostAxesCollection(this, axes);
            actualAxes.CollectionChanged += ActualAxesCollectionChanged;
            this.InternalActualAxes = actualAxes;
            this.ActualAxes = new ReadOnlyCollection<IAxis>(InternalActualAxes);

            // Create collection for LegendItems
            NoResetObservableCollection<UIElement> chartLegendItems = new AggregatedObservableCollection<UIElement>();
            _legendChildrenLegendAdapter.Collection = chartLegendItems;
            LegendItems = chartLegendItems;

            ISeriesHost host = this as ISeriesHost;
            host.GlobalSeriesIndexesInvalidated += OnGlobalSeriesIndexesInvalidated;

            ChartAreaChildren = new AggregatedObservableCollection<UIElement>();
            ChartAreaChildren.ChildCollections.Add(_edgeAxes);
            ChartAreaChildren.ChildCollections.Add(_backgroundElements);
            ChartAreaChildren.ChildCollections.Add(Series);
            ChartAreaChildren.ChildCollections.Add(_foregroundElements);

            _chartAreaChildrenListAdapter.Collection = ChartAreaChildren;

            // Create style dispenser
            StyleDispenser = new StyleDispenser();
        }

        /// <summary>
        /// Determines the location of an axis based on the existing axes in
        /// the chart.
        /// </summary>
        /// <param name="axis">The axis to determine the location of.</param>
        /// <returns>The location of the axis.</returns>
        private AxisLocation GetAutoAxisLocation(Axis axis)
        {
            if (axis.Orientation == AxisOrientation.X)
            {
                int numberOfTopAxes = InternalActualAxes.OfType<Axis>().Where(currentAxis => currentAxis.Location == AxisLocation.Top).Count();
                int numberOfBottomAxes = InternalActualAxes.OfType<Axis>().Where(currentAxis => currentAxis.Location == AxisLocation.Bottom).Count();
                return (numberOfBottomAxes > numberOfTopAxes) ? AxisLocation.Top : AxisLocation.Bottom;
            }
            else if (axis.Orientation == AxisOrientation.Y)
            {
                int numberOfLeftAxes = InternalActualAxes.OfType<Axis>().Where(currentAxis => currentAxis.Location == AxisLocation.Left).Count();
                int numberOfRightAxes = InternalActualAxes.OfType<Axis>().Where(currentAxis => currentAxis.Location == AxisLocation.Right).Count();
                return (numberOfLeftAxes > numberOfRightAxes) ? AxisLocation.Right : AxisLocation.Left;
            }
            else
            {
                return AxisLocation.Auto;
            }
        }

        /// <summary>
        /// Adds an axis to the ISeriesHost area.
        /// </summary>
        /// <param name="axis">The axis to add to the ISeriesHost area.</param>
        private void AddAxisToChartArea(Axis axis)
        {
            IRequireSeriesHost requiresSeriesHost = axis as IRequireSeriesHost;
            if (requiresSeriesHost != null)
            {
                requiresSeriesHost.SeriesHost = this;
            }

            if (axis.Location == AxisLocation.Auto)
            {
                axis.Location = GetAutoAxisLocation(axis);
            }

            SetEdge(axis);

            axis.LocationChanged += AxisLocationChanged;
            axis.OrientationChanged += AxisOrientationChanged;

            if (axis.Location != AxisLocation.Auto)
            {
                _edgeAxes.Add(axis);
            }
        }

        /// <summary>
        /// Rebuilds the chart area if an axis orientation is changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Information about the event.</param>
        private void AxisOrientationChanged(object sender, RoutedPropertyChangedEventArgs<AxisOrientation> args)
        {
            Axis axis = (Axis)sender;

            axis.Location = GetAutoAxisLocation(axis);
        }

        /// <summary>
        /// Sets the Edge property of an axis based on its location and
        /// orientation.
        /// </summary>
        /// <param name="axis">The axis to set the edge property of.</param>
        private static void SetEdge(Axis axis)
        {
            switch (axis.Location)
            {
                case AxisLocation.Bottom:
                    EdgePanel.SetEdge(axis, Edge.Bottom);
                    break;
                case AxisLocation.Top:
                    EdgePanel.SetEdge(axis, Edge.Top);
                    break;
                case AxisLocation.Left:
                    EdgePanel.SetEdge(axis, Edge.Left);
                    break;
                case AxisLocation.Right:
                    EdgePanel.SetEdge(axis, Edge.Right);
                    break;
            }
        }

        /// <summary>
        /// Rebuild the chart area if an axis location is changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Information about the event.</param>
        private void AxisLocationChanged(object sender, RoutedPropertyChangedEventArgs<AxisLocation> args)
        {
            Axis axis = (Axis)sender;

            if (args.NewValue == AxisLocation.Auto)
            {
                throw new InvalidOperationException(Properties.Resources.Chart_AxisLocationChanged_CantBeChangedToAutoWhenHostedInsideOfASeriesHost);
            }

            SetEdge(axis);

            _edgeAxes.Remove(axis);
            _edgeAxes.Add(axis);
        }

        /// <summary>
        /// Adds a series to the plot area and injects chart services.
        /// </summary>
        /// <param name="series">The series to add to the plot area.</param>
        private void AddSeriesToPlotArea(Series series)
        {
            series.SeriesHost = this;

            AggregatedObservableCollection<UIElement> chartLegendItems = this.LegendItems as AggregatedObservableCollection<UIElement>;
            int indexOfSeries = this.Series.IndexOf(series);
            chartLegendItems.ChildCollections.Insert(indexOfSeries, series.LegendItems);

            ISeriesHost host = series as ISeriesHost;
            if (host != null)
            {
                host.GlobalSeriesIndexesInvalidated += OnChildSeriesGlobalSeriesIndexesInvalidated;
            }
        }

        /// <summary>
        /// Builds the visual tree for the Chart control when a new template
        /// is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            // Call base implementation
            base.OnApplyTemplate();

            // Unhook events from former template parts
            if (null != ChartArea)
            {
                ChartArea.Children.Clear();
            }

            if (null != Legend)
            {
                Legend.Items.Clear();
                _legendChildrenLegendAdapter.TargetList = null;
            }

            // Access new template parts
            ChartArea = GetTemplateChild(ChartAreaName) as EdgePanel;

            Legend = GetTemplateChild(LegendName) as Legend;

            if (ChartArea != null)
            {
                _chartAreaChildrenListAdapter.TargetList = ChartArea.Children;
                _chartAreaChildrenListAdapter.Populate();
            }

            if (Legend != null)
            {
                _legendChildrenLegendAdapter.TargetList = Legend.Items;
                _legendChildrenLegendAdapter.Populate();
            }
        }

        /// <summary>
        /// Ensures that ISeriesHost is in a consistent state when axes collection is
        /// changed.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void ActualAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Axis axis in e.NewItems.OfType<Axis>())
                {
                    AddAxisToChartArea(axis);
                }
            }
            if (e.OldItems != null)
            {
                foreach (Axis axis in e.OldItems.OfType<Axis>())
                {
                    RemoveAxisFromChartArea(axis);
                }
            }
        }

        /// <summary>
        /// Causes the Chart to refresh the data obtained from its data source
        /// and render the resulting series.
        /// </summary>
        public void Refresh()
        {
            foreach (Series series in Series)
            {
                series.Refresh();
            }
        }

        /// <summary>
        /// Removes an axis from the Chart area.
        /// </summary>
        /// <param name="axis">The axis to remove from the ISeriesHost area.</param>
        private void RemoveAxisFromChartArea(Axis axis)
        {
            axis.LocationChanged -= AxisLocationChanged;
            axis.OrientationChanged -= AxisOrientationChanged;
            IRequireSeriesHost requiresSeriesHost = axis as IRequireSeriesHost;
            if (requiresSeriesHost != null)
            {
                requiresSeriesHost.SeriesHost = null;
            }

            _edgeAxes.Remove(axis);
        }

        /// <summary>
        /// Removes a series from the plot area.
        /// </summary>
        /// <param name="series">The series to remove from the plot area.
        /// </param>
        private void RemoveSeriesFromPlotArea(Series series)
        {
            AggregatedObservableCollection<UIElement> legendItemsList = LegendItems as AggregatedObservableCollection<UIElement>;
            legendItemsList.ChildCollections.Remove(series.LegendItems);

            ISeriesHost host = series as ISeriesHost;
            if (host != null)
            {
                host.GlobalSeriesIndexesInvalidated -= OnChildSeriesGlobalSeriesIndexesInvalidated;
            }
            series.SeriesHost = null;
        }

        /// <summary>
        /// Called when the ObservableCollection.CollectionChanged property
        /// changes.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void OnSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Clear ISeriesHost property of old Series
            if (null != e.OldItems)
            {
                foreach (Series series in e.OldItems)
                {
                    ISeriesHost host = series as ISeriesHost;
                    if (host != null)
                    {
                        foreach (IRequireGlobalSeriesIndex tracksGlobalIndex in host.GetDescendentSeries().OfType<IRequireGlobalSeriesIndex>())
                        {
                            tracksGlobalIndex.GlobalSeriesIndexChanged(null);
                        }
                    }
                    IRequireGlobalSeriesIndex require = series as IRequireGlobalSeriesIndex;
                    if (require != null)
                    {
                        require.GlobalSeriesIndexChanged(null);
                    }

                    RemoveSeriesFromPlotArea(series);
                }
            }

            // Set ISeriesHost property of new Series
            if (null != e.NewItems)
            {
                foreach (Series series in e.NewItems)
                {
                    AddSeriesToPlotArea(series);
                }
            }

            if (e.Action != NotifyCollectionChangedAction.Replace)
            {
                OnGlobalSeriesIndexesInvalidated(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Returns a rotating enumerator of Style objects that coordinates with 
        /// the style dispenser object to ensure that no two enumerators are
        /// currently on the same style if possible.  If the style
        /// dispenser is reset or its collection of styles is changed then
        /// the enumerators will also be reset.
        /// </summary>
        /// <param name="stylePredicate">A predicate that returns a value
        /// indicating whether to return a style.</param>
        /// <returns>An enumerator of styles.</returns>
        public IEnumerator<Style> GetStylesWhere(Func<Style, bool> stylePredicate)
        {
            return StyleDispenser.GetStylesWhere(stylePredicate);
        }

        /// <summary>
        /// Resets the styles dispensed by the chart.
        /// </summary>
        public void ResetStyles()
        {
            StyleDispenser.ResetStyles();
        }

        /// <summary>
        /// Method handles the event raised when a child series' global series
        /// indexes have changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Information about the event.</param>
        private void OnChildSeriesGlobalSeriesIndexesInvalidated(object sender, RoutedEventArgs args)
        {
            if (_globalSeriesIndicesInvalidated != null)
            {
                _globalSeriesIndicesInvalidated(sender, args);
            }
        }

        /// <summary>
        /// Updates the global indexes of all descendents that require a global
        /// index.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data.</param>
        private void OnGlobalSeriesIndexesInvalidated(object sender, RoutedEventArgs args)
        {
            UpdateGlobalIndexes();
        }

        /// <summary>
        /// Updates the global index property of all Series that track their
        /// global index.
        /// </summary>
        private void UpdateGlobalIndexes()
        {
            (this as ISeriesHost).GetDescendentSeries().OfType<IRequireGlobalSeriesIndex>().ForEachWithIndex(
                (seriesThatTracksGlobalIndex, index) =>
                {
                    seriesThatTracksGlobalIndex.GlobalSeriesIndexChanged(index);
                });
        }

        /// <summary>
        /// Gets or sets the Series host of the chart.
        /// </summary>
        /// <remarks>This will always return null.</remarks>
        ISeriesHost IRequireSeriesHost.SeriesHost { get; set; }

        /// <summary>
        /// Gets the axes collection of the chart.
        /// </summary>
        ObservableCollection<IAxis> ISeriesHost.Axes
        {
            get { return InternalActualAxes as ObservableCollection<IAxis>; }
        }

        /// <summary>
        /// Gets the Series collection of the chart.
        /// </summary>
        ObservableCollection<Series> ISeriesHost.Series
        {
            get { return (ObservableCollection<Series>)Series; }
        }

        /// <summary>
        /// This field is used to track listeners to the
        /// GlobalSeriesIndexesInvalidated event.
        /// </summary>
        private RoutedEventHandler _globalSeriesIndicesInvalidated;

        /// <summary>
        /// This event is raised when global Series indices are invalidated.
        /// </summary>
        event RoutedEventHandler ISeriesHost.GlobalSeriesIndexesInvalidated
        {
            add { _globalSeriesIndicesInvalidated += value; }
            remove { _globalSeriesIndicesInvalidated -= value; }
        }
    }
}