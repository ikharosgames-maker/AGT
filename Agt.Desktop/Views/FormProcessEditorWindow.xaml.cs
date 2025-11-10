using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
// --- USING DIREKTIVY (doplnìné a sjednocené) ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Dostupnost ViewModelù a modelù:
using Agt.Desktop.ViewModels;   // obsahuje StageVm, BlockVm (pøípadnì jiné VM)
using Agt.Desktop.Models;       // obsahuje PaletteItem, FieldComponentBase atd.

// Alias krátkých názvù (zabrání CS0246 i když ve zbytku souboru používáš krátké typy):
using StageVm = Agt.Desktop.ViewModels.StageVm;
using BlockVm = Agt.Desktop.ViewModels.BlockVm;
using PaletteItem = Agt.Desktop.Models.PaletteItem;

namespace Agt.Desktop.Views
{
    public partial class FormProcessEditorWindow : Window
    {
        public FormProcessEditorWindow()
        {
            InitializeComponent();
        }

        private void LoadPaletteFromLibrary(IBlockLibrary lib) { }

        private void OnAddStage(object sender, RoutedEventArgs e) {}
        private void OnResetZoom(object sender, RoutedEventArgs e) {}
        private void OnSnapAll(object sender, RoutedEventArgs e) {}
        private void OnPaletteDoubleClick(object sender, MouseButtonEventArgs e) {}
        private void PaletteList_PreviewMouseMove(object sender, MouseEventArgs e) {}
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {}
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {}
        private void Canvas_MouseMove(object sender, MouseEventArgs e) {}
        private void ZoomHost_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {}
        private void ZoomHost_MouseWheel(object sender, MouseWheelEventArgs e) {}
        private void Stage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {}
        private void Stage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {}
        private void Stage_MouseMove(object sender, MouseEventArgs e) {}
        private void Stage_Body_DragOver(object sender, DragEventArgs e) {}
        private void Stage_Drop(object sender, DragEventArgs e) {}
        private void Stage_InPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {}
        private void Stage_OutPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {}
        private void Stage_Resize_DragDelta(object sender, DragDeltaEventArgs e) {}
        private void OnBlockMouseDown(object sender, MouseButtonEventArgs e) {}
        private void OnBlockMouseMove(object sender, MouseEventArgs e) {}
        private void OnBlockMouseUp(object sender, MouseButtonEventArgs e) {}
        private void Port_MouseEnter(object sender, MouseEventArgs e) {}
        private void Port_MouseLeave(object sender, MouseEventArgs e) {}
        private void OnGroupFilterChanged(object sender, TextChangedEventArgs e) {}
        private void OnUserFilterChanged(object sender, TextChangedEventArgs e) {}
        private void OnAddGroups(object sender, RoutedEventArgs e) {}
        private void OnRemoveGroups(object sender, RoutedEventArgs e) {}
        private void OnAddUsers(object sender, RoutedEventArgs e) {}
        private void OnRemoveUsers(object sender, RoutedEventArgs e) {}
    }
}
