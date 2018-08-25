﻿using System.Windows.Controls;
using Orcus.Administration.Library.Views;

namespace FileExplorer.Administration.Resources
{
    public class VisualStudioImages : XamlIconsBase
    {
        public static Viewbox ListFolder() =>
            CreateImage(
                @"<Viewbox Width=""16"" Height=""16"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
   <Rectangle Width=""16"" Height=""16"">
    <Rectangle.Fill>
      <DrawingBrush>
        <DrawingBrush.Drawing>
          <DrawingGroup>
            <DrawingGroup.Children>
              <GeometryDrawing Brush=""#00FFFFFF"" Geometry=""F1M0,0L16,0 16,16 0,16z"" />
              <GeometryDrawing Brush=""{DynamicResource HaloBrush}"" Geometry=""F1M14.9961,4.5L14.9961,12.5C14.9961,13.327,14.3231,14,13.4961,14L11.0001,14 11.0001,16 9.99999999997669E-05,16 9.99999999997669E-05,12.5 9.99999999997669E-05,7 9.99999999997669E-05,2.5C9.99999999997669E-05,1.673,0.6731,1,1.5001,1L9.6101,1 10.6101,3 13.4961,3C14.3231,3,14.9961,3.673,14.9961,4.5"" />
              <GeometryDrawing Brush=""#FFDCB67A"" Geometry=""F1M2,4L2,3 8.374,3 8.874,4z M13.496,4L10,4 9.992,4 8.992,2 1.5,2C1.225,2,1,2.224,1,2.5L1,7 11,7 11,13 13.496,13C13.773,13,13.996,12.776,13.996,12.5L13.996,4.5C13.996,4.224,13.773,4,13.496,4"" />
              <GeometryDrawing Brush=""{DynamicResource DarkBrush1}"" Geometry=""F1M9,10L4,10 4,9 9,9z M9,12L4,12 4,11 9,11z M9,14L4,14 4,13 9,13z M2,13L3,13 3,14 2,14z M2,11L3,11 3,12 2,12z M2,9L3,9 3,10 2,10z M1,15L10,15 10,8 1,8z"" />
              <GeometryDrawing Brush=""{DynamicResource LightBrush2}"" Geometry=""F1M2,3L8.374,3 8.874,4 2,4z M9,10L4,10 4,9 9,9z M9,12L4,12 4,11 9,11z M9,14L4,14 4,13 9,13z M2,13L3,13 3,14 2,14z M2,11L3,11 3,12 2,12z M2,9L3,9 3,10 2,10z"" />
            </DrawingGroup.Children>
          </DrawingGroup>
        </DrawingBrush.Drawing>
      </DrawingBrush>
    </Rectangle.Fill>
  </Rectangle>
</Viewbox>");
    }
}