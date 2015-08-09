﻿//Project: Hotspotizer (https://github.com/mbaytas/hotspotizer)
//File: MainWindow.VisualizerUtils.cs
//Version: 20150809

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfApplication.Helpers;
using WpfApplication.Models;

namespace WpfApplication
{
  public partial class MainWindow : Window
  {

    #region --- Methods ---

    public bool CanPlay(object parameter)
    {
      if (kinect == null) return false;
      else if (GestureCollection.Count > 0 && kinect.Status == Microsoft.Kinect.KinectStatus.Connected) return true;
      else return false;
    }

    public void Play(object parameter) //TODO: make method smaller (refactor into multiple methods)
    {
      CollisionTimes = new List<List<DateTime>>();
      CollisionStates = new List<JointCollisionStates[]>();
      CollisionHighlights_3D = new Model3DGroup();
      CollisionHighlights_Front = new Model3DGroup();
      CollisionHighlights_Side = new Model3DGroup();
      foreach (Gesture g in GestureCollection)
      {
        CollisionTimes.Add(new List<DateTime>());
        foreach (GestureFrame f in g.Frames) CollisionTimes.Last().Add(new DateTime());
        CollisionStates.Add(new JointCollisionStates[2] { JointCollisionStates.OutThere, JointCollisionStates.OutThere });
        CollisionHighlights_3D.Children.Add(new Model3DGroup());
        CollisionHighlights_Front.Children.Add(new Model3DGroup());
        CollisionHighlights_Side.Children.Add(new Model3DGroup());
      }
      HotspotCellsModelVisual3D_Hit_Visualizer.Content = CollisionHighlights_3D;

      // Mark gesture cells in 3D Grid
      Model3DGroup modelGroup = new Model3DGroup();
      foreach (Gesture g in GestureCollection)
      {
        foreach (GestureFrame f in g.Frames)
        {
          // Create material
          SolidColorBrush materialBrush = new SolidColorBrush()
          {
            Color = Visualizer_GestureColors[GestureCollection.IndexOf(g) % Visualizer_GestureColors.Length].Color,
            Opacity = 0.1 + ((double)(g.Frames.IndexOf(f) + 1) / (double)g.Frames.Count) * 0.6
          };
          DiffuseMaterial material = new DiffuseMaterial(materialBrush);

          foreach (GestureFrameCell fc in f.FrontCells.Where(fc => fc.IsHotspot == true))
          {
            int fcIndex = Array.IndexOf(f.FrontCells, fc);
            foreach (GestureFrameCell sc in f.SideCells.Where(
                     sc => sc.IsHotspot == true && (int)(Array.IndexOf(f.SideCells, sc) / 20) == (int)(fcIndex / 20)))
            {
              // Init mesh
              MeshBuilder meshBuilder = new MeshBuilder(false, false);
              // Make cube and add to mesh
              double y = (fc.LeftCM + fc.RightCM) / 2;
              double z = (fc.TopCM + fc.BottomCM) / 2;
              double x = (sc.LeftCM + sc.RightCM) / 2;
              Point3D cubeCenter = new Point3D(x, y, z);
              meshBuilder.AddBox(cubeCenter, 15, 15, 15);
              // Create and freeze mesh
              var mesh = meshBuilder.ToMesh(true);
              // Create models
              modelGroup.Children.Add(new GeometryModel3D(mesh, material));
            }
          }
        }
        HotspotCellsModelVisual3D_Visualizer.Content = modelGroup;
      }

      ShowVisualizer();
      EnableKinect_Visualizer();
      DisableKeyboardControl_Visualizer(); //we don't want to consume emulated keyboard events
    }

    public void CloseVisualizer(object parameter)
    {
      DisableKinect_Visualizer();
      HideVisualizer();
    }

    private void ShowVisualizer()
    {
      TheEditor.Visibility = Visibility.Hidden;
      TheVisualizer.Visibility = Visibility.Visible;
      EditorOverlay.Visibility = Visibility.Hidden;
      ManagerOverlay.Visibility = Visibility.Visible; // Hide Manager
    }

    private void HideVisualizer()
    {
      EditorOverlay.Visibility = Visibility.Visible;
      TheEditor.Visibility = Visibility.Hidden;
      TheVisualizer.Visibility = Visibility.Hidden;
      ManagerOverlay.Visibility = Visibility.Hidden; // Show Manager
    }

    private void EnableKinect_Visualizer()
    {
      if (kinect != null)
      {
        kinect.SkeletonStream.Enable();
        kinect.SkeletonFrameReady += SkeletonFrameReady_Draw3D_Visualizer;
        kinect.SkeletonFrameReady += SkeletonFrameReady_Detect_Visualizer;
        kinect.Start();
      }
    }

    private void DisableKinect_Visualizer()
    {
      if (kinect != null)
      {
        kinect.Stop();
        kinect.SkeletonFrameReady -= SkeletonFrameReady_Draw3D_Visualizer;
        kinect.SkeletonFrameReady -= SkeletonFrameReady_Detect_Visualizer;
        kinect.SkeletonStream.Disable();
      }
    }

    private void DisableKeyboardControl_Visualizer()
    {
      EventLogic.RemoveRoutedEventHandlers(ViewPort3D_Visualizer.CameraController, HelixToolkit.Wpf.CameraController.KeyDownEvent);
    }

    #endregion

  }
}
