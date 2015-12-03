﻿//Project: Hotspotizer (https://github.com/mbaytas/hotspotizer)
//File: MainWindow.SpeechUtils.cs
//Version: 20151202

using Hotspotizer.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Hotspotizer
{
  public partial class MainWindow : Window
  {
    #region --- Constants ---

    private const double DEFAULT_SPEECH_RECOGNITION_CONFIDENCE_THRESHOLD = 0.7;

    #endregion

    #region --- Fields ---

    public ISpeechSynthesis speechSynthesis;
    public ISpeechRecognition speechRecognition;
    public ISpeechRecognitionKinect speechRecognitionKinect;

    private double SpeechRecognitionConfidenceThreshold = DEFAULT_SPEECH_RECOGNITION_CONFIDENCE_THRESHOLD;

    #endregion

    #region --- Methods ---

    public void LoadSpeechSynthesisPlugin()
   {
     Lazy<ISpeechSynthesis> plugin = PluginsCatalog.mefContainer.GetExports<ISpeechSynthesis>("SpeechSynthesis").FirstOrDefault();
     speechSynthesis = (plugin != null) ? plugin.Value : null;
     if(speechSynthesis != null)
       speechSynthesis.Init();
   }

    public void LoadSpeechRecognitionPlugin()
    {
      Lazy<ISpeechRecognitionKinect> plugin1 = PluginsCatalog.mefContainer.GetExports<ISpeechRecognitionKinect>("SpeechRecognitionKinect").FirstOrDefault();
      speechRecognition = speechRecognitionKinect = (plugin1 != null) ? plugin1.Value : null;

      if (speechRecognition == null) //SpeechRecognitionKinect plugin couldn't be loaded, try to fallback to the SpeechRecognition one (which uses the default audio source as input)
      {
        Lazy<ISpeechRecognition> plugin2 = PluginsCatalog.mefContainer.GetExports<ISpeechRecognition>("SpeechRecognition").FirstOrDefault();
        speechRecognition = (plugin2 != null) ? plugin2.Value : null;
      }

      if (speechRecognition != null)
      {
        speechRecognition.Init();
        StartSpeechRecognition();
      }
    }

    private void StartSpeechRecognition()
    {
      if (speechRecognition == null)
        return;

      try
      {
        string grammarsFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Grammars", "SRGS");
        speechRecognition.LoadGrammar(new FileStream(Path.Combine(grammarsFolder, "SpeechGrammar_Manager_en.xml"), FileMode.Open), "Manager");
        speechRecognition.LoadGrammar(new FileStream(Path.Combine(grammarsFolder, "SpeechGrammar_Editor_en.xml"), FileMode.Open), "Editor");
        speechRecognition.LoadGrammar(new FileStream(Path.Combine(grammarsFolder, "SpeechGrammar_Visualizer_en.xml"), FileMode.Open), "Visualizer");

        speechRecognition.Recognized += SpeechRecognition_Recognized;
        speechRecognition.NotRecognized += SpeechRecognition_NotRecognized;

        /*
        //For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model.
        //This will prevent recognition accuracy from degrading over time.
        speechRecognition.AcousticModelAdaptation = false;
        */

        if (speechRecognitionKinect != null)
          speechRecognitionKinect.SetInputToKinectSensor(); //if it can't find a Kinect sensor that call will fallback to default audio device for input
        else
          speechRecognition.SetInputToDefaultAudioDevice();

        speechRecognition.Start();
      }
      catch(Exception e)
      {
        speechRecognitionKinect = null;
        speechRecognition = null;
        MessageBox.Show(e.Message);
      }
    }

    #endregion

    #region --- Events ---

    private void SpeechRecognition_Recognized(object sender, SpeechRecognitionEventArgs e)
    {
      Dictionary<string, ICommand> commands;
      if (EditorVisible) commands = commands_Editor;
      else if (VisualizerVisible) commands = commands_Visualizer;
      else commands = commands_Manager;

      ICommand cmd;
      if ((e.confidence >= SpeechRecognitionConfidenceThreshold) &&
          commands.TryGetValue(e.command, out cmd) &&
          cmd.CanExecute(null)
         ) //must use && to do short-circuit boolean evaluation here
        cmd.Execute(null);
    }

    private void SpeechRecognition_NotRecognized(object sender, EventArgs e)
    {
      //TODO: maybe show some hint about which are the supported voice commands
    }

    #endregion

  }
}
