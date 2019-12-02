//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <description>
// Demonstrates usage of the Kinect Tooling APIs, including basic record/playback functionality
// </description>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Win32;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Tools;
    using System.Windows.Threading;
    using System.Windows.Controls.Primitives;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        private List<Snippet> snippets;
        private int backCode;
        private int armsCode;
        private int legsCode;
        private int owasCode;

        private readonly int skip_value = 10;
        private readonly int reverse_value = 10;

        private ObservableCollection<BoxBody> bodies;

        private bool userIsDraggingSlider = false;
        private bool userDraggedSlider = false;

        private bool _recordingSnippet = false;
        private bool _fileLoaded = false;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isStopped = false;

        private bool snippet_aborted = false;

        private TimeSpan timePlayed;
        private TimeSpan newStartingPoint;
        private TimeSpan duration = new TimeSpan(0, 0, 0);

        private string lastFile = string.Empty;


        /// <summary> Number of playback iterations </summary>
        private uint loopCount = 0;

        /// when these Properties change, they trigger the UpdateState Method, to update the UI
        /// <summary> Indicates if a snippet is currently recording </summary>
        public bool RecordingSnippet
        {
            get { return _recordingSnippet; }
            set
            {
                if (_recordingSnippet != value)
                {
                    _recordingSnippet = value;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }
        /// <summary> Indicates if a recording is currently loaded </summary>
        public bool FileLoaded
        {
            get { return _fileLoaded; }
            set
            {
                if (_fileLoaded != value)
                {
                    _fileLoaded = value;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }
        /// <summary> Indicates if a playback is currently playing </summary>
        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }
        /// <summary> Indicates if a playback is currently paused </summary>
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }

        /// <summary> Indicates if a playback is stopped </summary>
        public bool IsStopped
        {
            get { return _isStopped; }
            set
            {
                if (_isStopped != value)
                {
                    _isStopped = value;
                    this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
                }
            }
        }

        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);      


        /// <summary> Active Kinect sensor </summary>
        private KinectSensor kinectSensor = null;

        /// <summary> Current kinect sesnor status text to display </summary>
        private string kinectStatusText = string.Empty;

        /// <summary>
        /// Current record/playback status text to display
        /// </summary>
        private string recordPlayStatusText = string.Empty;

        /// <summary>
        /// Current record/playback status text to display
        /// </summary>
        private string currentTimeText = string.Empty;

        /// <summary>
        /// Infrared visualizer
        /// </summary>
        private KinectIRView kinectIRView = null;

        /// <summary>
        /// Depth visualizer
        /// </summary>
        private KinectDepthView kinectDepthView = null;

        /// <summary>
        /// BodyIndex visualizer
        /// </summary>
        private KinectBodyIndexView kinectBodyIndexView = null;

        /// <summary>
        /// Body visualizer
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.bodies = new ObservableCollection<BoxBody>();

            this.snippets = new List<Snippet>();

            // initialize the components (controls) of the window
            this.InitializeComponent();

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // create the IR visualizer
            this.kinectIRView = new KinectIRView(this.kinectSensor);

            // create the Depth visualizer
            this.kinectDepthView = new KinectDepthView(this.kinectSensor);

            // create the BodyIndex visualizer
            this.kinectBodyIndexView = new KinectBodyIndexView(this.kinectSensor);

            // create the Body visualizer
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);

            // add Eventhandler which updates the comboBox 
            this.kinectBodyView.BodiesChanged += new EventHandler<BodiesArrivedEventArgs>(BodiesArrived);

            // add Eventhandler which updates the StartSnippet Button
            this.cbBodies.SelectionChanged += new SelectionChangedEventHandler(OnCbBodiesChanged);

            // set data context for display in UI
            this.DataContext = this;
            this.kinectIRViewbox.DataContext = this.kinectIRView;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current OwasCode to display
        /// </summary>
        public int OwasCode
        {
            get
            {
                return this.owasCode;
            }

            set
            {
                if (this.owasCode != value)
                {
                    this.owasCode = value;

                    // notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OwasCode"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string KinectStatusText
        {
            get
            {
                return this.kinectStatusText;
            }

            set
            {
                if (this.kinectStatusText != value)
                {
                    this.kinectStatusText = value;

                    // notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("KinectStatusText"));
                }
            }
        }


        /// <summary>
        /// Gets or sets the current status text to display for the record/playback features
        /// </summary>
        public string RecordPlaybackStatusText
        {
            get
            {
                return this.recordPlayStatusText;
            }

            set
            {
                if (this.recordPlayStatusText != value)
                {
                    this.recordPlayStatusText = value;

                    // notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecordPlaybackStatusText"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current time text of the playback
        /// </summary>
        public string CurrentTimeText
        {
            get
            {
                return this.currentTimeText;
            }

            set
            {
                if (this.currentTimeText != value)
                {
                    this.currentTimeText = value;

                    // notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTimeText"));
                }
            }
        }


        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            if (this.kinectIRView != null)
            {
                this.kinectIRView.Dispose();
                this.kinectIRView = null;
            }

            if (this.kinectDepthView != null)
            {
                this.kinectDepthView.Dispose();
                this.kinectDepthView = null;
            }

            if (this.kinectBodyIndexView != null)
            {
                this.kinectBodyIndexView.Dispose();
                this.kinectBodyIndexView = null;
            }

            if (this.kinectBodyView != null)
            {
                this.kinectBodyView.Dispose();
                this.kinectBodyView = null;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.kinectIRView != null)
            {
                this.kinectIRView.Dispose();
                this.kinectIRView = null;
            }

            if (this.kinectDepthView != null)
            {
                this.kinectDepthView.Dispose();
                this.kinectDepthView = null;
            }

            if (this.kinectBodyIndexView != null)
            {
                this.kinectBodyIndexView.Dispose();
                this.kinectBodyIndexView = null;
            }

            if (this.kinectBodyView != null)
            {
                this.kinectBodyView.Dispose();
                this.kinectBodyView = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the event in which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }


        /// <summary>
        /// Changes the internal properties based on the KStudiPlaybackState 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleStateChange(Object sender, EventArgs e)
        {
            KStudioPlayback playback = (KStudioPlayback) sender;
            Debug.WriteLine("state: " + playback.State);

            if(playback != null)
            {
                if (playback.State == KStudioPlaybackState.Playing)
                {
                    FileLoaded = true;
                    IsPlaying = true;
                    IsPaused = false;
                    IsStopped = false;
                }
                else if (playback.State == KStudioPlaybackState.Paused)
                {
                    FileLoaded = true;
                    IsPlaying = false;
                    IsPaused = true;
                    IsStopped = false;
                }
                else if (playback.State == KStudioPlaybackState.Stopped)
                {
                    FileLoaded = false;
                    IsPlaying = false;
                    IsPaused = false;
                    IsStopped = true;
                }
            }
        }

        /// <summary>
        /// Plays back a .xef file to the Kinect sensor
        /// </summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param>
        private void PlaybackClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.StateChanged += new EventHandler(HandleStateChange);
                    playback.LoopCount = this.loopCount;
                    playback.EndBehavior = KStudioPlaybackEndBehavior.Stop;
                    playback.StartPaused();

                    this.FileLoaded = true;
                    this.timePlayed = new TimeSpan(0, 0, 0);

                    this.duration = playback.Duration;

                    // Update UI Timer once at start
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        UpdateTimer(timePlayed);
                    }));

                    

                    while (playback.State != KStudioPlaybackState.Stopped)
                    {

                        while (playback.State == KStudioPlaybackState.Playing)
                        {
                            // Check if stopped, then check if paused, else keep playing
                            if (IsStopped)
                            {
                                playback.Stop();
                            }
                            else if (IsPaused)
                            {
                                playback.Pause();
                            }
                            else
                            {
                                // Update the UI Timer to current playTime
                                timePlayed = playback.CurrentRelativeTime;
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                    UpdateTimer(timePlayed);
                                }));
                            }
                        }

                        while (playback.State == KStudioPlaybackState.Paused)
                        {
                            if (IsStopped)
                            {
                                playback.Stop();
                            }
                            else if (!IsPaused)
                            {
                                playback.Resume();
                            }
                            else
                            {
                                // Happens only when the user FINISHED dragging the slider
                                if (userDraggedSlider)
                                {
                                    userDraggedSlider = false;
                                    playback.SeekByRelativeTime(newStartingPoint);
                                    playback.Resume();
                                }
                            }
                        }
                    }
                }
                
                client.DisconnectFromService();
            }
        }

       /// <summary>
       /// Happens when bodies from the KinectBodyView frame arrive
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void BodiesArrived(object sender, BodiesArrivedEventArgs e)
        {
            //sets the combobox itemsource if not already set
            if (cbBodies.ItemsSource == null)
            {
                cbBodies.ItemsSource = bodies;
                cbBodies.DisplayMemberPath = "Name";
            }

            // Updates the ComboBox observable collection
            foreach (Body body in e.Bodies)
            {
                BoxBody boxBody = new BoxBody(body);
                // only adds a body when the TrackingId isnt already in the List
                if (!bodies.Any(n => n.TrackindId == boxBody.TrackindId))
                {
                    bodies.Add(boxBody);
                }
            }

            // removes all bodies which only exist in bodies and not in e.Bodies
            bodies.Remove(a => !e.Bodies.Exists(b => a.TrackindId == b.TrackingId));

            if (RecordingSnippet)
            {
                // when the tracked body gets lost, abort the snippet recording
                if (cbBodies.SelectedItem == null)
                {
                    snippet_aborted = true;
                    RecordSnippet(null,null);
                    return;
                }

                // adds the selected body from the combobox to the trackedBodyList from the recording snippet
                BoxBody selectedBody = cbBodies.SelectedItem as BoxBody;
                if (selectedBody != null)
                {
                    Body trackedBody = e.Bodies.First(n => n.TrackingId == selectedBody.TrackindId);
                    if(trackedBody != null)
                    {
                        snippets[snippets.Count - 1].AddTrackedBody(trackedBody);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the text UI and slider UI
        /// </summary>
        /// <param name="time"></param>
        private void UpdateTimer(TimeSpan time)
        {
            CurrentTimeText = "Current Time: " + time.ToString(@"hh\:mm\:ss") + "/" + duration.ToString(@"hh\:mm\:ss");
            
            if (!userIsDraggingSlider)
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = duration.TotalSeconds;
                this.sliProgress.Value = time.TotalSeconds;
            }
            
        }

        /// <summary>
        /// Changes the UI based on the Property States
        /// </summary>
        private void UpdateState()
        {
            if (IsPlaying)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                PlayPauseImage.Source = new BitmapImage(new Uri(@"Images\controls\pause.png", UriKind.Relative));
                StopButton.IsEnabled = true;
                SkipButton.IsEnabled = true;
                ReverseButton.IsEnabled = true;
                this.RecordPlaybackStatusText = "Playback is playing";
                sliProgress.IsEnabled = true;
            }
            else if (IsPaused)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                PlayPauseImage.Source = new BitmapImage(new Uri(@"Images\controls\play.png", UriKind.Relative));
                StopButton.IsEnabled = true;
                SkipButton.IsEnabled = true;
                ReverseButton.IsEnabled = true;
                this.RecordPlaybackStatusText = "Playback is paused";
                sliProgress.IsEnabled = true;
            }
            else if(IsStopped)
            {
                LoadButton.IsEnabled = true;
                PlayPauseButton.IsEnabled = false;
                StopButton.IsEnabled = false;
                SkipButton.IsEnabled = false;
                ReverseButton.IsEnabled = false;
                sliProgress.IsEnabled = false;

                this.RecordPlaybackStatusText = "Playback is stopped";
                this.CurrentTimeText = "";

                kinectBodyViewbox.DataContext = null;
                kinectIRViewbox.DataContext = null;
                cbBodies.ItemsSource = null;
            }

            if (RecordingSnippet)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                SnippetButton.IsEnabled = true;
                sliProgress.IsEnabled = false;
            }
            else
            {
                if (!IsStopped) sliProgress.IsEnabled = true;
            }
        }

        /// <summary>
        /// Launches the OpenFileDialog window to help user find/select an event file for playback
        /// </summary>
        /// <returns>Path to the event file selected by the user</returns>
        private string OpenFileForPlayback()
        {
            string fileName = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog
            {
                FileName = this.lastFile,
                DefaultExt = Properties.Resources.XefExtension, // Default file extension
                Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter // Filter files by extension 
            };
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// When the slider value changes, the timer gets updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (userIsDraggingSlider)
            {
                UpdateTimer(TimeSpan.FromSeconds(sliProgress.Value));
            }
        }

        /// <summary>
        /// pauses the video when user starts dragging the slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
            IsPaused = true;
        }

        /// <summary>
        /// Set the newStartingPoint, so the playback moves to that point in the playback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            userDraggedSlider = true;
            newStartingPoint = TimeSpan.FromSeconds(sliProgress.Value);
        }

        /// <summary>
        /// Updates the OWAS-Code based on the RadioButtons
        /// </summary>
        private void RadioButton_Check(object sender,RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.IsChecked.Value)
            {
                switch (radioButton.Name)
                {
                    case "back1":
                        backCode = 1;
                        break;
                    case "back2":
                        backCode = 2;
                        break;
                    case "back3":
                        backCode = 3;
                        break;
                    case "back4":
                        backCode = 4;
                        break;
                    case "arms1":
                        armsCode = 1;
                        break;
                    case "arms2":
                        armsCode = 2;
                        break;
                    case "arms3":
                        armsCode = 3;
                        break;
                    case "legs1":
                        legsCode = 1;
                        break;
                    case "legs2":
                        legsCode = 2;
                        break;
                    case "legs3":
                        legsCode = 3;
                        break;
                    case "legs4":
                        legsCode = 4;
                        break;
                    case "legs5":
                        legsCode = 5;
                        break;
                    case "legs6":
                        legsCode = 6;
                        break;
                    case "legs7":
                        legsCode = 7;
                        break;
                }
            }
            OwasCode = int.Parse(backCode.ToString() + armsCode.ToString() + legsCode.ToString()); ;
        }

        /// <summary>
        /// Updates the SnippetButton when the selected item changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnCbBodiesChanged(object sender, SelectionChangedEventArgs e)
        {
            if((sender as ComboBox).SelectedItem != null)
            {
                SnippetButton.IsEnabled = true;
            }
            else
            {
                if (!RecordingSnippet)
                {
                    SnippetButton.IsEnabled = false;
                }
            }
        }

        // If the Views are null, set new instances
        // If the filePath is legit, start the playback
        private void Load(Object sender, ExecutedRoutedEventArgs e)
        {
            if (!FileLoaded || IsStopped)
            {
                string filePath = this.OpenFileForPlayback();

                if (this.kinectIRViewbox.DataContext == null)
                {
                    // create the IR visualizer
                    this.kinectIRView = new KinectIRView(this.kinectSensor);

                    // create the Body visualizer
                    this.kinectBodyView = new KinectBodyView(this.kinectSensor);

                    this.kinectIRViewbox.DataContext = this.kinectIRView;
                    this.kinectBodyViewbox.DataContext = this.kinectBodyView;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    this.lastFile = filePath;

                    if (Path.GetExtension(lastFile) != ".xef")
                    {
                        MessageBox.Show("Only .xef files are supported.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Start running the playback asynchronously
                    OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                    playback.BeginInvoke(filePath, null, null);
                }
            }
        }

        /// <summary>
        /// Change the Play/Pause state of the playback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPause(Object sender, ExecutedRoutedEventArgs e)
        {
            if (FileLoaded && !IsPaused)
            {
                IsPaused = true;
            }
            else
            {
                IsPaused = false;
            }
        }

        /// <summary>
        /// Stops the playback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stop(Object sender, ExecutedRoutedEventArgs e)
        {
            if (FileLoaded && !RecordingSnippet)
            {
                IsStopped = true;
            }
        }

        /// <summary>
        /// Starts or Stops the recording of the snippet
        /// Changes GUI accordingly
        /// </summary>
        private void RecordSnippet(Object sender, ExecutedRoutedEventArgs e)
        {
            if (FileLoaded)
            {
                if (RecordingSnippet)
                {
                    BorderRecording.BorderBrush = Brushes.Transparent;
                    RecordingSnippet = false;

                    //Gets last snippet of the list (which is the one recording at the moment) and adds data
                    snippets[snippets.Count - 1].Ending = TimeSpan.FromSeconds(sliProgress.Value);
                    snippets[snippets.Count - 1].CodeBack = backCode;
                    snippets[snippets.Count - 1].CodeArms = armsCode;
                    snippets[snippets.Count - 1].CodeLegs = legsCode;

                    if (snippet_aborted)
                    {
                        IsPaused = true;
                        MessageBox.Show("The tracked body got lost. The recording stopped and no .xml file will be created.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        snippets[snippets.Count - 1].PrintXml();
                    }
                }
                else
                {
                    if (cbBodies.SelectedItem != null)
                    {
                        BorderRecording.BorderBrush = Brushes.Red;
                        snippet_aborted = false;
                        RecordingSnippet = true;

                        Snippet snippet = new Snippet(TimeSpan.FromSeconds(sliProgress.Value));
                        snippets.Add(snippet);
                    }
                    else
                    {
                        IsPaused = true;
                        MessageBox.Show("Please select a body to track.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Uses the slider mechanics to skip forward the playback based the skip_value ( in seconds)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Skip(Object sender, ExecutedRoutedEventArgs e)
        {
            if (FileLoaded)
            {
                //the pause allows the playback loop to go into paused state, where the skip happens based on the newStartingPoint
                if (TimeSpan.FromSeconds(sliProgress.Value + skip_value) < duration && !RecordingSnippet)
                {
                    IsPaused = true;
                    userDraggedSlider = true;
                    newStartingPoint = TimeSpan.FromSeconds(sliProgress.Value + skip_value);
                }
            }
        }

        /// <summary>
        /// Uses the slider mechanics to reverse back the playback based on the reverse_value ( in seconds)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Reverse(Object sender, ExecutedRoutedEventArgs e)
        {
            if (FileLoaded)
            {
                if (!RecordingSnippet)
                {
                    IsPaused = true;
                    userDraggedSlider = true;

                    if ((sliProgress.Value - reverse_value) < 0)
                    {
                        newStartingPoint = TimeSpan.FromSeconds(0);
                    }
                    else
                    {
                        newStartingPoint = TimeSpan.FromSeconds(sliProgress.Value - reverse_value);
                    }
                }
            }
        }
    }
}
