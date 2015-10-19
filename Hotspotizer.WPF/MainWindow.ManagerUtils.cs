﻿//Project: Hotspotizer (https://github.com/mbaytas/hotspotizer)
//File: MainWindow.ManagerUtils.cs
//Version: 20150915

using System.Windows;
using System.Windows.Controls;
using Hotspotizer.Models;
using GlblRes = Hotspotizer.Properties.Resources;
using Microsoft.Win32;
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Collections.Generic;

namespace Hotspotizer
{
  public partial class MainWindow : IManager
  {

    #region --- Methods ---

    #region IManager methods

    public void CreateNewGestureCollection()
    {
      if (MessageBox.Show(GlblRes.DiscardGestureCollectionConfirmation,
                          GlblRes.CreateNewGestureCollection, MessageBoxButton.YesNo)
          == MessageBoxResult.Yes)
        GestureCollection.Clear();
    }

    public void LoadGestureCollection()
    {
      OpenFileDialog openDialog = new OpenFileDialog() { Filter = GlblRes.HotspotizerFileFilter };

      // Read and load if dialog returns OK
      if (openDialog.ShowDialog() == true)
      {
        string json = File.ReadAllText(openDialog.FileName);
        // DeserializeObject() does not appear to correctly deserialize Gesture objects
        // Below is a kinda-dirty solution around that
        List<Gesture> sourceList = JsonConvert.DeserializeObject<List<Gesture>>(json);
        GestureCollection.Clear();

        foreach (Gesture sourceGesture in sourceList)
        {
          Gesture targetGesture = new Gesture()
          {
            Name = sourceGesture.Name,
            Command = new ObservableCollection<Key>(sourceGesture.Command),
            Hold = sourceGesture.Hold,
            Joint = sourceGesture.Joint
          };

          targetGesture.Frames.Clear();

          foreach (GestureFrame sourceFrame in sourceGesture.Frames)
          {
            GestureFrame targetFrame = new GestureFrame();
            for (int i = 0; i < 400; i++)
            {
              targetFrame.FrontCells[i] = sourceFrame.FrontCells[i];
              targetFrame.SideCells[i] = sourceFrame.SideCells[i];
            }
            targetGesture.Frames.Add(targetFrame);
          }
          GestureCollection.Add(targetGesture);
        }
      }
    }

    public void SaveGestureCollection()
    {
      SaveFileDialog saveDialog = new SaveFileDialog() { Filter = GlblRes.HotspotizerFileFilter };

      // Write if dialog returns OK
      if (saveDialog.ShowDialog() == true)
        File.WriteAllText(saveDialog.FileName, JsonConvert.SerializeObject(GestureCollection));
    }

    public bool CanSaveGestureCollection()
    {
      return (GestureCollection.Count > 0);
    }

    public void AddNewGesture()
    {
      // Clear the initial state store
      ExGesture = null;

      // Make the new gesture and name it properly
      Gesture g = MakeNewGesture();
      g.Name = GlblRes.NewGesture;
      while (GestureCollection.Where(x => x.Name.StartsWith(g.Name)).Count() > 0)
        g.Name += " " + Convert.ToString(GestureCollection.Where(x => x.Name.StartsWith(g.Name)).Count() + 1);

      // Add the new gesture to the Gesture Collection
      GestureCollection.Add(g);

      // Go
      TheWorkspace.DataContext = g;
      ShowEditor();
    }

    public void EditGesture(Gesture g)
    {
      // Store the initial state of the gesture being edited
      ExGesture = DeepCopyGesture(g);
      // Go
      TheWorkspace.DataContext = g;
      ShowEditor();
    }

    public void DeleteGesture(Gesture g)
    {
      if (MessageBox.Show(string.Format(GlblRes.DeleteGestureFromCollectionConfirmation, g.Name),
                    GlblRes.DeleteGestureFromCollection, MessageBoxButton.YesNo)
          == MessageBoxResult.Yes)
        GestureCollection.Remove(g);
    }

    #endregion

    #endregion

    #region --- Events ---

    public void EditGestureButton_Click(object sender, RoutedEventArgs e)
    {
      Button b = (Button)sender;
      Gesture g = (Gesture)b.DataContext;
      EditGesture(g);
    }

    public void DeleteGestureButton_Click(object sender, RoutedEventArgs e)
    {
      Button b = (Button)sender;
      Gesture g = (Gesture)b.DataContext;
      DeleteGesture(g);
    }

    #endregion

  }
}