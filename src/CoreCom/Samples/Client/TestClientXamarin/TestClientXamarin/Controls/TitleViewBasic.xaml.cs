using System;
using System.Windows.Input;
using TestClientXamarin.Helpers;
using Xamarin.Forms;

namespace TestClientXamarin.Controls
{

    public partial class TitleViewBasic : ContentView
    {
        private const int _widthIcon = 30;
        private DateTime _lastClickTimeLeft = DateTime.Now.AddDays(-1);
        private DateTime _lastClickTimeRight = DateTime.Now.AddDays(-1);
        private TapGestureRecognizer gestureRecognizerActionBack;

        public TitleViewBasic()
        {
            InitializeComponent();


            //aligng with first char in status bar left and right
            //HandleInsideTitleView=true(default) this is when the control is inside a navigationpage
            if (Device.RuntimePlatform == Device.iOS)
                this.MainGrid.Margin = new Thickness(0, 0, -8, 0);
            else
                this.MainGrid.Margin = new Thickness(0, 0, 8, 0);

            gestureRecognizerActionBack = new TapGestureRecognizer();
            var gestureRecognizerActionRight = new TapGestureRecognizer();
            var gestureRecognizerActionLeft = new TapGestureRecognizer();

            NavigationBackStackLayout.GestureRecognizers.Add(gestureRecognizerActionBack);


            ActionRightImageCtrl.GestureRecognizers.Add(gestureRecognizerActionRight);
            ActionRightLabelCtrl.GestureRecognizers.Add(gestureRecognizerActionRight);
            ActionRightStackLayout.GestureRecognizers.Add(gestureRecognizerActionRight);

            ActionLeftImageCtrl.GestureRecognizers.Add(gestureRecognizerActionLeft);
            ActionLeftLabelCtrl.GestureRecognizers.Add(gestureRecognizerActionLeft);
            ActionLeftStackLayout.GestureRecognizers.Add(gestureRecognizerActionLeft);
            NotificationCountFrameCtrl.GestureRecognizers.Add(gestureRecognizerActionRight);
            NotificationCountLableCtrl.GestureRecognizers.Add(gestureRecognizerActionRight);

            gestureRecognizerActionRight.Tapped += (s, e) =>
            {
                if (ActionRightCommand != null && ActionRightCommand.CanExecute(null))
                {
                    var span = DateTime.Now - _lastClickTimeRight;
                    if (span.TotalMilliseconds > 2000)
                    {
                        ActionRightCommand.Execute(null);
                        //simulerar klick
                        this.IsEnabled = false;
                        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                        {
                            this.IsEnabled = true;
                            return false; // True = Repeat again, False = Stop the timer
                        });
                    }
                    _lastClickTimeRight = DateTime.Now;
                }
            };

            gestureRecognizerActionLeft.Tapped += (s, e) =>
            {
                if (ActionLeftCommand != null && ActionLeftCommand.CanExecute(null))
                {
                    var span = DateTime.Now - _lastClickTimeLeft;
                    if (span.TotalMilliseconds > 2000)
                    {
                        ActionLeftCommand.Execute(null);
                        //simulerar klick
                        this.IsEnabled = false;
                        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                        {
                            this.IsEnabled = true;
                            return false; // True = Repeat again, False = Stop the timer
                        });
                    }
                    _lastClickTimeLeft = DateTime.Now;
                }
            };
        }

        #region Set Title Text Margin 


        public void SetTitleTextMargin()
        {
            //Reason why we have +15 on Android on right side is because when we navigate so that we have a back button visible 
            //Android gets an added left margin which results in a misplaced title.So we compensate for this here
            //this applay only for when we are InsideTitleView = true

            //När du kommer in i en navigationpage så kommer back pile för långt till höger på andrid detta går inte att fixa
            //https://github.com/xamarin/Xamarin.Forms/issues/4848

            //heslt skulle manvilja använda with på stacklayouten men de är -1 
            int left = this.NavigationBackStackLayout.IsVisible ? 46 : 0;
            left += string.IsNullOrEmpty(this.ActionLeftIconSource) ? 0 : 55;
            left += ActionLeftText.Length > 0 ? ActionLeftText.Length * 20 : 0;

            int right = string.IsNullOrEmpty(this.ActionRightIconSource) ? 0 : 55;
            right += ActionRightText.Length > 0 ? ActionRightText.Length * 20 : 0; //20px/bokstav
            int androidleftMaring = 0;

            if (InsideTitleView && Device.RuntimePlatform == Device.Android)
            {
                androidleftMaring = 15;
            }
            if (left > right)
            {
                this.TitleTextCtrl.Margin = Device.RuntimePlatform == Device.Android ? new Thickness((left / 2), 0, (left / 2) + androidleftMaring, 0) : new Thickness(left / 2, 0, left / 2, 0);
            }
            else if (right > 0)
            {
                this.TitleTextCtrl.Margin = Device.RuntimePlatform == Device.Android ? new Thickness((right / 2), 0, (right / 2) + androidleftMaring, 0) : new Thickness(right / 2, 0, right / 2, 0);
            }
            else
            {
                this.TitleTextCtrl.Margin = Device.RuntimePlatform == Device.Android ? new Thickness(3, 0, 3 + androidleftMaring, 0) : new Thickness(3, 0, 3, 0);

            }
        }
        #endregion

        #region Navigation Back Propertys
        // ************* NAVIGATION BACK *******************
        public bool NavigateBackIsVisible
        {
            get { return (bool)GetValue(NavigateBackIsVisibleProperty); }
            set { SetValue(NavigateBackIsVisibleProperty, value); }
        }
        public Color NavigateBackTextColor
        {
            get { return (Color)base.GetValue(NavigateBackTextColorProperty); }
            set { base.SetValue(NavigateBackTextColorProperty, value); }
        }
        public static BindableProperty NavigateBackIsVisibleProperty = BindableProperty.Create(propertyName: nameof(NavigateBackIsVisible),
         returnType: typeof(bool), declaringType: typeof(ContentView),
         defaultValue: false, defaultBindingMode: BindingMode.OneWay,
         propertyChanged: HandleNavigateBackIsVisiblePropertyChanged);

        public static readonly BindableProperty NavigateBackTextColorProperty = BindableProperty.Create(propertyName: nameof(NavigateBackTextColor),
                                                 returnType: typeof(Color), declaringType: typeof(TitleViewBasic),
                                                 defaultValue: Helpers.StyleResources.WhiteColor(), defaultBindingMode: BindingMode.OneWay,
                                                 propertyChanged: NavigateBackTextColorPropertyChanged);

        private static void NavigateBackTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
                control.NavigateBackImageCtrl.TextColor = (Color)newValue;
        }
        private static void HandleNavigateBackIsVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TitleViewBasic targetView;

            targetView = (TitleViewBasic)bindable;
            //Här sker logiken föra att ställa om knappen utseende
            if (targetView != null && newValue != null)
            {
                bool setvalue = (bool)newValue;
                targetView.NavigationBackStackLayout.IsVisible = setvalue;
                targetView.NavigationBackStackLayout.WidthRequest = setvalue ? _widthIcon : -1;
                targetView.SetTitleTextMargin();
            }
        }


        #endregion
        #region Title Text

        // *********** TITLE TEXT **********************
        public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(propertyName: nameof(TitleText),
                                                       returnType: typeof(string), declaringType: typeof(TitleViewBasic),
                                                       defaultValue: "", defaultBindingMode: BindingMode.TwoWay,
                                                       propertyChanged: TitleTextPropertyChanged);

        public string TitleText
        {
            get { return base.GetValue(TitleTextProperty).ToString(); }
            set { base.SetValue(TitleTextProperty, value); }
        }

        private static void TitleTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
                control.TitleTextCtrl.Text = newValue.ToString();
        }
        #endregion

        #region Title Text Color

        // *********** TITLE TEXT COLOR **********************
        public static readonly BindableProperty TitleTextColorProperty = BindableProperty.Create(propertyName: nameof(TitleTextColor),
                                                       returnType: typeof(Color), declaringType: typeof(TitleViewBasic),
                                                       defaultValue: Helpers.StyleResources.WhiteColor(), defaultBindingMode: BindingMode.TwoWay,
                                                       propertyChanged: TitleTextColorPropertyChanged);

        public Color TitleTextColor
        {
            get { return (Color)base.GetValue(TitleTextColorProperty); }
            set { base.SetValue(TitleTextColorProperty, value); }
        }

        private static void TitleTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
                control.TitleTextCtrl.TextColor = (Color)newValue;
        }
        #endregion
        #region Action Left Propertys

        #endregion
        #region Action Left Propertys
        // *********** Action Left Propertys **********************
        public static readonly BindableProperty ActionLeftCommandProperty = BindableProperty.Create(nameof(ActionLeftCommand), typeof(ICommand), typeof(TitleViewBasic), null);
        public static readonly BindableProperty ActionRightCommandProperty = BindableProperty.Create(nameof(ActionRightCommand), typeof(ICommand), typeof(TitleViewBasic), null);


        public ICommand ActionLeftCommand
        {
            get { return (ICommand)GetValue(ActionLeftCommandProperty); }
            set { SetValue(ActionLeftCommandProperty, value); }
        }
        public ICommand ActionRightCommand
        {
            get { return (ICommand)GetValue(ActionRightCommandProperty); }
            set { SetValue(ActionRightCommandProperty, value); }
        }

        public static readonly BindableProperty ActionLeftIconSourceProperty = BindableProperty.Create(propertyName: nameof(ActionLeftIconSource),
         returnType: typeof(String), declaringType: typeof(TitleViewBasic), defaultValue: "",
          defaultBindingMode: BindingMode.OneWay, propertyChanged: ActionLeftIconSourcePropertyChanged);

        public static readonly BindableProperty ActionRightIconSourceProperty = BindableProperty.Create(propertyName: nameof(ActionRightIconSource),
         returnType: typeof(String), declaringType: typeof(TitleViewBasic), defaultValue: "",
          defaultBindingMode: BindingMode.OneWay, propertyChanged: ActionRightIconSourcePropertyChanged);

        public string ActionLeftIconSource
        {
            get { return base.GetValue(ActionLeftIconSourceProperty).ToString(); }
            set { base.SetValue(ActionLeftIconSourceProperty, value); }
        }
        public string ActionRightIconSource
        {
            get { return base.GetValue(ActionRightIconSourceProperty).ToString(); }
            set { base.SetValue(ActionRightIconSourceProperty, value); }
        }

        private static void ActionLeftIconSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {

            TitleViewBasic targetView = (TitleViewBasic)bindable;
            if (targetView != null && newValue != null)
            {
                try
                {
                    bool setvalue = !string.IsNullOrEmpty((string)newValue);
                    targetView.ActionLeftStackLayout.IsVisible = setvalue;
                    targetView.ActionLeftStackLayout.WidthRequest = setvalue ? _widthIcon : -1;
                    targetView.ActionLeftImageCtrl.Text = ((string)newValue);
                    targetView.SetTitleTextMargin();
                }
                catch (Exception e)
                {
                    App.ConsoleWriteLineDebug(e);
                }

            }
        }
        private static void ActionRightIconSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {

            TitleViewBasic targetView = (TitleViewBasic)bindable;
            if (targetView != null && newValue != null)
            {
                try
                {
                    bool setvalue = !string.IsNullOrEmpty((string)newValue);
                    targetView.ActionRightStackLayout.IsVisible = setvalue;
                    targetView.ActionRightStackLayout.WidthRequest = setvalue ? _widthIcon : -1;
                    targetView.ActionRightImageCtrl.Text = ((string)newValue);
                    targetView.SetTitleTextMargin();
                }
                catch (Exception e)
                {
                    App.ConsoleWriteLineDebug(e);
                }

            }
        }
        public static readonly BindableProperty ActionLeftTextProperty = BindableProperty.Create(propertyName: nameof(ActionLeftText),
                                                     returnType: typeof(string), declaringType: typeof(TitleViewBasic),
                                                     defaultValue: "", defaultBindingMode: BindingMode.TwoWay,
                                                     propertyChanged: ActionLeftTextPropertyChanged);

        public static readonly BindableProperty ActionRightTextProperty = BindableProperty.Create(propertyName: nameof(ActionRightText),
                                                     returnType: typeof(string), declaringType: typeof(TitleViewBasic),
                                                     defaultValue: "", defaultBindingMode: BindingMode.TwoWay,
                                                     propertyChanged: ActionRightTextPropertyChanged);

        public static readonly BindableProperty NotificationCountProperty = BindableProperty.Create(propertyName: nameof(NotificationCount),
                                                     returnType: typeof(int), declaringType: typeof(TitleViewBasic),
                                                     defaultValue: -1, defaultBindingMode: BindingMode.TwoWay,
                                                     propertyChanged: NotificationCountPropertyChanged);

        public static readonly BindableProperty ActionRightTextColorProperty = BindableProperty.Create(propertyName: nameof(ActionRightTextColor),
                                                  returnType: typeof(Color), declaringType: typeof(TitleViewBasic),
                                                  defaultValue: Helpers.StyleResources.WhiteColor(), defaultBindingMode: BindingMode.TwoWay,
                                                  propertyChanged: ActionRightTextColorPropertyChanged);
        public string ActionLeftText
        {
            get { return base.GetValue(ActionLeftTextProperty).ToString(); }
            set { base.SetValue(ActionLeftTextProperty, value); }
        }
        public string ActionRightText
        {
            get { return base.GetValue(ActionRightTextProperty).ToString(); }
            set { base.SetValue(ActionRightTextProperty, value); }
        }
        public string NotificationCount
        {
            get { return base.GetValue(NotificationCountProperty).ToString(); }
            set { base.SetValue(NotificationCountProperty, value); }
        }

        public Color ActionRightTextColor
        {
            get { return (Color)base.GetValue(ActionRightTextColorProperty); }
            set { base.SetValue(ActionRightTextColorProperty, value); }
        }

        private static void ActionLeftTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
            {
                control.ActionLeftStackLayout.IsVisible = true;
                control.ActionLeftLabelCtrl.Text = newValue.ToString();
                control.SetTitleTextMargin();

            }
        }
        private static void ActionRightTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
            {
                control.ActionRightStackLayout.IsVisible = true;
                control.ActionRightLabelCtrl.Text = newValue.ToString();
                control.SetTitleTextMargin();
            }
        }
        private static void NotificationCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
            {
                if (Convert.ToInt32(newValue) > 0)
                {
                    control.NotificationCountFrameCtrl.IsVisible = true;
                    control.NotificationCountLableCtrl.Text = newValue.ToString();
                }
                else
                {
                    control.NotificationCountFrameCtrl.IsVisible = false;
                    control.NotificationCountLableCtrl.Text = "0";
                }

            }
            else
            {
                control.NotificationCountFrameCtrl.IsVisible = false;
                control.NotificationCountLableCtrl.Text = "0";
            }
        }
        private Color _actionRightTextColor;
        private static void ActionRightTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TitleViewBasic)bindable;
            if (newValue != null)
                control.ActionRightLabelCtrl.TextColor = (Color)newValue;
        }

        public bool ActionLeftCanExecute
        {
            get { return (bool)GetValue(ActionLeftCanExecuteProperty); }
            set { SetValue(ActionLeftCanExecuteProperty, value); }
        }
        public bool ActionRightCanExecute
        {
            get { return (bool)GetValue(ActionRightCanExecuteProperty); }
            set { SetValue(ActionRightCanExecuteProperty, value); }
        }
        public static BindableProperty ActionLeftCanExecuteProperty = BindableProperty.Create(propertyName: nameof(ActionLeftCanExecute),
        returnType: typeof(bool), declaringType: typeof(ContentView),
        defaultValue: true, defaultBindingMode: BindingMode.OneWay,
        propertyChanged: HandleActionLeftCanExecutePropertyChanged);

        public static BindableProperty ActionRightCanExecuteProperty = BindableProperty.Create(propertyName: nameof(ActionRightCanExecute),
         returnType: typeof(bool), declaringType: typeof(ContentView),
         defaultValue: true, defaultBindingMode: BindingMode.OneWay,
         propertyChanged: HandleActionRightCanExecutePropertyChanged);


        private static void HandleActionLeftCanExecutePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TitleViewBasic targetView;

            targetView = (TitleViewBasic)bindable;
            //Här sker logiken föra att ställa om knappen utseende
            if (targetView != null && newValue != null)
            {
                bool currentvalue = (bool)newValue;
                targetView.ActionLeftImageCtrl.TextColor = currentvalue ? StyleResources.WhiteColor() : StyleResources.Primary400Color();
                targetView.ActionLeftLabelCtrl.TextColor = currentvalue ? StyleResources.WhiteColor() : StyleResources.Primary400Color();


                targetView.ActionLeftImageCtrl.IsEnabled = currentvalue;
                targetView.ActionLeftLabelCtrl.IsEnabled = currentvalue;
                targetView.ActionLeftStackLayout.IsEnabled = currentvalue;

            }
        }
        private static void HandleActionRightCanExecutePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TitleViewBasic targetView;

            targetView = (TitleViewBasic)bindable;
            if (targetView != null && newValue != null)
            {
                bool currentvalue = (bool)newValue;

                targetView.ActionRightImageCtrl.TextColor = currentvalue ? StyleResources.WhiteColor() : StyleResources.Primary400Color();
                targetView.ActionRightLabelCtrl.TextColor = currentvalue ? StyleResources.WhiteColor() : StyleResources.Primary400Color();

                targetView.ActionRightImageCtrl.IsEnabled = currentvalue;
                targetView.ActionRightLabelCtrl.IsEnabled = currentvalue;
                targetView.ActionRightStackLayout.IsEnabled = currentvalue;

            }
        }

        public bool InsideTitleView
        {
            get { return (bool)GetValue(InsideTitleViewProperty); }
            set
            {

                SetValue(InsideTitleViewProperty, value);
            }
        }
        public static BindableProperty InsideTitleViewProperty = BindableProperty.Create(propertyName: nameof(InsideTitleView),
       returnType: typeof(bool), declaringType: typeof(ContentView),
       defaultValue: true, defaultBindingMode: BindingMode.OneWay,
       propertyChanged: HandleInsideTitleViewPropertyChanged);

        private static void HandleInsideTitleViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TitleViewBasic targetView;

            targetView = (TitleViewBasic)bindable;
            if (targetView != null && newValue != null)
            {
                bool currentvalue = (bool)newValue;
                if (currentvalue)
                {
                    targetView.MainGrid.Margin = new Thickness(0, 0, 0, 0);
                }
                else
                {
                    if (Device.RuntimePlatform == Device.iOS)
                        targetView.MainGrid.Margin = new Thickness(9, 0, 6, 0);
                    else
                        targetView.MainGrid.Margin = new Thickness(14, 0, 10, 0);
                }

            }
        }

    }
    #endregion
}
        

