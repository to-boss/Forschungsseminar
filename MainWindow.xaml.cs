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

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        private List<Snippet> snippets;
        private int backCode;
        private int armsCode;
        private int legsCode;

        private bool userIsDraggingSlider = false;
        private bool userDraggedSlider = false;

        private bool recordingSnippet = false;

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isLoaded = false;

        /// <summary> Indicates if a playback is currently in progress </summary>
        private bool isPlaying = false;

        /// <summary> Indicates if a playback is currently paused </summary>
        /// is true, because playback always starts paused
        private bool isPaused = true;

        /// <summary> Indicates if a playback is stopped </summary>
        private bool isStopped = false;

        private TimeSpan timePlayed;
        private TimeSpan startingPoint;
        private TimeSpan duration = new TimeSpan(0,0,0);

        private string lastFile = string.Empty;


        /// <summary> Number of playback iterations </summary>
        private uint loopCount = 0;

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

            // set data context for display in UI
            this.DataContext = this;
            this.kinectIRViewbox.DataContext = this.kinectIRView;
            this.kinectDepthViewbox.DataContext = this.kinectDepthView;
            this.kinectBodyIndexViewbox.DataContext = this.kinectBodyIndexView;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("KinectStatusText"));
                    }
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
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("RecordPlaybackStatusText"));
                    }
                }
            }
        }

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
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimeText"));
                    }
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
        /// Handles the user clicking on the Play button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        void HandleStateChange(Object sender, EventArgs e)
        {
            KStudioPlayback playback = (KStudioPlayback) sender;
            Debug.WriteLine("state: " + playback.State);

            if (playback.State == KStudioPlaybackState.Playing)
            {
                isLoaded = true;
                isPlaying = true;
                isPaused = false;
                isStopped = false;
            }
            else if (playback.State == KStudioPlaybackState.Paused)
            {
                isLoaded = true;
                isPlaying = false;
                isPaused = true;
                isStopped = false;
            }
            else if (playback.State == KStudioPlaybackState.Stopped)
            {
                isLoaded = false;
                isPlaying = false;
                isPaused = false;
                isStopped = true;
            }

            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
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

                    this.isLoaded = true;
                    duration = playback.Duration;

                    var watch = new Stopwatch();
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        UpdateTimer(new TimeSpan(0,0,0));
                    }));

                    while (playback.State != KStudioPlaybackState.Stopped)
                    {

                        while (playback.State == KStudioPlaybackState.Playing)
                        {
                            //Check if stopped, then check if paused, else keep playing
                            if (isStopped)
                            {
                                watch.Stop();
                                playback.Stop();
                            }
                            else if (isPaused)
                            {
                                watch.Stop();
                                playback.Pause();
                            }
                            else
                            {
                                //timePlayed = watch.Elapsed;
                                timePlayed = playback.CurrentRelativeTime;
                                
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                    UpdateTimer(timePlayed);
                                }));
                            }
                        }

                        while (playback.State == KStudioPlaybackState.Paused)
                        {
                            if (isStopped)
                            {
                                watch.Stop();
                                playback.Stop();
                            }
                            else if (!isPaused)
                            {
                                watch.Start();
                                playback.Resume();
                            }
                            else
                            {
                                if (userDraggedSlider)
                                {
                                    playback.SeekByRelativeTime(startingPoint);
                                    userDraggedSlider = false;
                                    playback.Resume();
                                    Debug.WriteLine("userDraggedSlider paused ausgeführt");
                                }
                                else
                                {
                                    Thread.Sleep(500);
                                }
                            }
                        }
                    }

                    //resets playback to beginning
                    //playback.InPointByRelativeTime = playback.StartRelativeTime;
                }
                
                client.DisconnectFromService();
            }

            // Update the UI after the background playback task has completed
            //this.isPlaying = false;
            //this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            Debug.WriteLine("Stopped");
        }

       
        private void UpdateTimer(TimeSpan time)
        {
            this.CurrentTimeText = "Current Time: " + time.ToString(@"hh\:mm\:ss") + "/" + duration.ToString(@"hh\:mm\:ss");
            
            
            if (!userIsDraggingSlider)
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = duration.TotalSeconds;
                this.sliProgress.Value = time.TotalSeconds;
            }
            
        }

        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (isPlaying)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SnippetButton.IsEnabled = true;
                this.RecordPlaybackStatusText = "Playback is playing";
            }
            else if (isPaused)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SnippetButton.IsEnabled = true;
                this.RecordPlaybackStatusText = "Playback is paused";
            }
            else if (isLoaded)
            {
                LoadButton.IsEnabled = false;
                PlayPauseButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SnippetButton.IsEnabled = true;
                this.RecordPlaybackStatusText = "Playback is loaded";
            }
            else if(isStopped)
            {
                LoadButton.IsEnabled = true;
                PlayPauseButton.IsEnabled = false;
                StopButton.IsEnabled = false;
                SnippetButton.IsEnabled = false;
                this.RecordPlaybackStatusText = "Playback is stopped";
                this.CurrentTimeText = "";
            }
        }

        /// <summary>
        /// Launches the OpenFileDialog window to help user find/select an event file for playback
        /// </summary>
        /// <returns>Path to the event file selected by the user</returns>
        private string OpenFileForPlayback()
        {
            string fileName = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = this.lastFile;
            dlg.DefaultExt = Properties.Resources.XefExtension; // Default file extension
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter; // Filter files by extension 
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isStopped = true;
            PlayPauseButton.Content = "Play";
            kinectBodyIndexViewbox.DataContext = null;
            kinectBodyViewbox.DataContext = null;
            kinectDepthViewbox.DataContext = null;
            kinectIRViewbox.DataContext = null;
        }

        private void SliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
            isPaused = true;
        }

        private void SliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            startingPoint = TimeSpan.FromSeconds(sliProgress.Value);
            userDraggedSlider = true;
            //mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = this.OpenFileForPlayback();

            if (this.kinectIRViewbox.DataContext == null)
            {
                // create the IR visualizer
                this.kinectIRView = new KinectIRView(this.kinectSensor);

                // create the Depth visualizer
                this.kinectDepthView = new KinectDepthView(this.kinectSensor);

                // create the BodyIndex visualizer
                this.kinectBodyIndexView = new KinectBodyIndexView(this.kinectSensor);

                // create the Body visualizer
                this.kinectBodyView = new KinectBodyView(this.kinectSensor);

                this.kinectIRViewbox.DataContext = this.kinectIRView;
                this.kinectDepthViewbox.DataContext = this.kinectDepthView;
                this.kinectBodyIndexViewbox.DataContext = this.kinectBodyIndexView;
                this.kinectBodyViewbox.DataContext = this.kinectBodyView;
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;

                // Start running the playback asynchronously
                OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                playback.BeginInvoke(filePath, null, null);
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                PlayPauseButton.Content = "Resume";
                isPaused = true;
            }
            else
            {
                PlayPauseButton.Content = "Pause";
                isPaused = false;
            }
        }

        private void SliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (userIsDraggingSlider)
            {
                UpdateTimer(TimeSpan.FromSeconds(sliProgress.Value));
            }
            //UpdateTimer(TimeSpan.FromSeconds(sliProgress.Value));
            //lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// Starts or Stops the recording of the snippet
        /// Changes GUI accordingly
        /// </summary>
        private void SnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (recordingSnippet)
            {
                radioButtonsStack.IsEnabled = false;
                recordingSnippet = false;
                SnippetButton.Content = "StartSnippet";

                //Gets last snippet of the list (which is the one recording at the moment) and adds data
                snippets[snippets.Count - 1].Ending = TimeSpan.FromSeconds(sliProgress.Value);
                snippets[snippets.Count - 1].CodeBack = backCode;
                snippets[snippets.Count - 1].CodeArms = armsCode;
                snippets[snippets.Count - 1].CodeLegs = legsCode;

                Debug.WriteLine(snippets[snippets.Count - 1].InfoAsString());
            }
            else
            {
                radioButtonsStack.IsEnabled = true;
                recordingSnippet = true;
                SnippetButton.Content = "StopSnippet";

                Snippet snippet = new Snippet(TimeSpan.FromSeconds(sliProgress.Value));
                snippets.Add(snippet);
            }
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
        }
    }
}
