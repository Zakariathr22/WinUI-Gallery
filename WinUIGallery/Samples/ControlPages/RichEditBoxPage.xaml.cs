//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using WinUIGallery.Helpers;

namespace WinUIGallery.ControlPages;

public sealed partial class RichEditBoxPage : Page
{
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
    public static extern IntPtr GetActiveWindow();
    private Windows.UI.Color currentColor = Microsoft.UI.Colors.Green;
    private List<MathSymbol> Symbols = MathModeHelper.GetSymbols();
    private List<MathStructure> BasicStructures;
    private List<MathStructure> Integrals;
    private List<MathStructure> LargeOperators;
    private List<MathStructure> Accents;
    private List<MathStructure> LimitAndFunctions;
    private List<MathStructure> Matrices;

    public RichEditBoxPage()
    {
        this.InitializeComponent();

        MathEditor.TextDocument.SetMathMode(RichEditMathMode.MathOnly);
        MathSymbolsItems.ItemsSource = Symbols;
        mathEditor2.TextDocument.SetMathMode(RichEditMathMode.MathOnly);
    }

    private void Menu_Opening(object sender, object e)
    {
        CommandBarFlyout myFlyout = sender as CommandBarFlyout;
        if (myFlyout != null && myFlyout.Target == REBCustom)
        {
            AppBarButton myButton = new AppBarButton
            {
                Command = new StandardUICommand(StandardUICommandKind.Share)
            };
            myFlyout.PrimaryCommands.Add(myButton);
        }
        else
        {
            CommandBarFlyout muxFlyout = sender as CommandBarFlyout;
            if (muxFlyout != null && muxFlyout.Target == REBCustom)
            {
                AppBarButton myButton = new AppBarButton
                {
                    Command = new StandardUICommand(StandardUICommandKind.Share)
                };
                muxFlyout.PrimaryCommands.Add(myButton);
            }
        }

    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        // Open a text file.
        FileOpenPicker open = new FileOpenPicker();
        open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        open.FileTypeFilter.Add(".rtf");

        // When running on win32, FileOpenPicker needs to know the top-level hwnd via IInitializeWithWindow::Initialize.
        if (Window.Current == null)
        {
            IntPtr hwnd = GetActiveWindow();
            WinRT.Interop.InitializeWithWindow.Initialize(open, hwnd);
        }

        StorageFile file = await open.PickSingleFileAsync();

        if (file != null)
        {
            using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                // Load the file into the Document property of the RichEditBox.
                editor.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
            }
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        FileSavePicker savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        // Dropdown of file types the user can save the file as
        savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });

        // Default file name if the user does not type one in or select a file to replace
        savePicker.SuggestedFileName = "New Document";

        // When running on win32, FileSavePicker needs to know the top-level hwnd via IInitializeWithWindow::Initialize.
        if (Window.Current == null)
        {
            IntPtr hwnd = GetActiveWindow();
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
        }

        StorageFile file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            // Prevent updates to the remote version of the file until we
            // finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);
            // write to file
            using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                editor.Document.SaveToStream(TextGetOptions.FormatRtf, randAccStream);
            }

            // Let Windows know that we're finished changing the file so the
            // other app can update the remote version of the file.
            FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
            if (status != FileUpdateStatus.Complete)
            {
                Windows.UI.Popups.MessageDialog errorBox =
                    new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                await errorBox.ShowAsync();
            }
        }
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        editor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        editor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Extract the color of the button that was clicked.
        Button clickedColor = (Button)sender;
        var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
        var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

        editor.Document.Selection.CharacterFormat.ForegroundColor = color;

        fontColorButton.Flyout.Hide();
        editor.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        currentColor = color;
    }

    private void FindBoxHighlightMatches()
    {
        FindBoxRemoveHighlights();

        Windows.UI.Color highlightBackgroundColor = (Windows.UI.Color)App.Current.Resources["SystemColorHighlightColor"];
        Windows.UI.Color highlightForegroundColor = (Windows.UI.Color)App.Current.Resources["SystemColorHighlightTextColor"];

        string textToFind = findBox.Text;
        if (textToFind != null)
        {
            ITextRange searchRange = editor.Document.GetRange(0, 0);
            while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
            {
                searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
            }
        }
    }

    private void FindBoxRemoveHighlights()
    {
        ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
        SolidColorBrush defaultBackground = editor.Background as SolidColorBrush;
        SolidColorBrush defaultForeground = editor.Foreground as SolidColorBrush;

        documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
        documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
    }

    private void Editor_GotFocus(object sender, RoutedEventArgs e)
    {
        editor.Document.GetText(TextGetOptions.UseCrlf, out _);
        
        // reset colors to correct defaults for Focused state
        ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
        SolidColorBrush background = (SolidColorBrush)App.Current.Resources["TextControlBackgroundFocused"];

        if (background != null)
        {
            documentRange.CharacterFormat.BackgroundColor = background.Color;
        }
    }

    private void REBCustom_Loaded(object sender, RoutedEventArgs e)
    {
        // Prior to UniversalApiContract 7, RichEditBox did not have a default ContextFlyout set.
        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
        {
            // customize the menu that opens on text selection
            REBCustom.SelectionFlyout.Opening += Menu_Opening;

            // also customize the context menu to match selection menu
            REBCustom.ContextFlyout.Opening += Menu_Opening;
        }
    }

    private void REBCustom_Unloaded(object sender, RoutedEventArgs e)
    {
        // Prior to UniversalApiContract 7, RichEditBox did not have a default ContextFlyout set.
        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
        {
            REBCustom.SelectionFlyout.Opening -= Menu_Opening;
            REBCustom.ContextFlyout.Opening -= Menu_Opening;
        }
    }

    private void Editor_TextChanged(object sender, RoutedEventArgs e)
    {
        if (editor.Document.Selection.CharacterFormat.ForegroundColor != currentColor)
        {
            editor.Document.Selection.CharacterFormat.ForegroundColor = currentColor;
        }
    }

    private void SymbolDataTemplate_Loading(FrameworkElement sender, object args)
    {
        if (sender is Grid grid)
        {
            int index = MathSymbolsItems.GetElementIndex(grid);

            if (index % 2 == 0)
            {
                grid.Style = (Style)MathModeExample.Resources["ItemStyle1"];
            }
            else
            {
                grid.Style = (Style)MathModeExample.Resources["ItemStyle2"];
            }
        }
    }

    private void InsertSymbolBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button insertionBtn)
        {
            if (MathEditor == null) return;

            MathEditor.Focus(FocusState.Programmatic);

            var selection = MathEditor.Document.Selection;
            if (selection != null)
            {
                InputSender.SendUnicodeText(insertionBtn.Tag.ToString());
            }
        }
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if(sender is SelectorBar selectorBar)
        {
            if (selectorBar.SelectedItem.Tag == null) return;

            if (selectorBar.SelectedItem.Tag.ToString() == "Symbols")
            {
                SymbolsTable.Visibility = Visibility.Visible;
                StructuresTable.Visibility = Visibility.Collapsed;
            }
            else
            {
                StructuresTable.Visibility = Visibility.Visible;
                SymbolsTable.Visibility = Visibility.Collapsed;
                switch (selectorBar.SelectedItem.Tag.ToString())
                {
                    case "Structures":
                        if (BasicStructures == null)
                        {
                            BasicStructures = MathModeHelper.GetStructures(StructuresCategory.BasicStructures);
                        }
                        MathStructuresItems.ItemsSource = BasicStructures;
                        break;
                    case "Integrals":
                        if (Integrals == null)
                        {
                            Integrals = MathModeHelper.GetStructures(StructuresCategory.Integrals);
                        }
                        MathStructuresItems.ItemsSource = Integrals;
                        break;
                    case "LargeOperators":
                        if (LargeOperators == null)
                        {
                            LargeOperators = MathModeHelper.GetStructures(StructuresCategory.LargeOperators);
                        }
                        MathStructuresItems.ItemsSource = LargeOperators;
                        break;
                    case "Accents":
                        if (Accents == null)
                        {
                            Accents = MathModeHelper.GetStructures(StructuresCategory.Accents);
                        }
                        MathStructuresItems.ItemsSource = Accents;
                        break;
                    case "LimitAndFunctions":
                        if (LimitAndFunctions == null)
                        {
                            LimitAndFunctions = MathModeHelper.GetStructures(StructuresCategory.LimitAndFunctions);
                        }
                        MathStructuresItems.ItemsSource = LimitAndFunctions;
                        break;
                    case "Matrices":
                        if (Matrices == null)
                        {
                            Matrices = MathModeHelper.GetStructures(StructuresCategory.Matrices);
                        }
                        MathStructuresItems.ItemsSource = Matrices;
                        break;
                }
            }
        }
    }

    private void StructureDataTemplate_Loading(FrameworkElement sender, object args)
    {
        if (sender is Grid grid)
        {
            int index = MathStructuresItems.GetElementIndex(grid);

            if (index % 2 == 0)
            {
                grid.Style = (Style)MathModeExample.Resources["ItemStyle1"];
            }
            else
            {
                grid.Style = (Style)MathModeExample.Resources["ItemStyle2"];
            }
        }
    }

    private void LightThemeImage_Loaded(object sender, RoutedEventArgs e)
    {
        if(sender is Image image)
        {
            if(image.ActualTheme == ElementTheme.Light)
            {
                image.Visibility = Visibility.Visible;
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void DarkThemeImage_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Image image)
        {
            if (image.ActualTheme == ElementTheme.Dark)
            {
                image.Visibility = Visibility.Visible;
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void LightThemeImage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (sender is Image image)
        {
            if (image.ActualTheme == ElementTheme.Light)
            {
                image.Visibility = Visibility.Visible;
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void DarkThemeImage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (sender is Image image)
        {
            if (image.ActualTheme == ElementTheme.Dark)
            {
                image.Visibility = Visibility.Visible;
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void InsertStructureBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button insertionBtn)
        {
            if (MathEditor == null) return;

            MathEditor.Focus(FocusState.Programmatic);

            var selection = MathEditor.Document.Selection;
            if (selection != null)
            {
                InputSender.SendUnicodeText(insertionBtn.Tag.ToString());
            }
        }
    }

    private void mathEditor2_TextChanged(object sender, RoutedEventArgs e)
    {
        string extractedMathML;
        mathEditor2.Document.GetMathML(out extractedMathML);
        if (!string.IsNullOrEmpty(extractedMathML))
        {
            MathmlPresenter.Code = MathModeHelper.FormatMathML(extractedMathML);
        }
        else
        {
            MathmlPresenter.Code = "<!-- No MathML content -->";
        }
    }

    private void SetMathmlEquationBtn_Click(object sender, RoutedEventArgs e)
    {
        string equationMathML = "<mml:math xmlns:mml=\"http://www.w3.org/1998/Math/MathML\" display=\"block\">" +
            "\r\n  <mml:mi mathcolor=\"#000000\">x</mml:mi>\r\n" +
            "  <mml:mo mathcolor=\"#000000\">\u2208</mml:mo>\r\n" +
            "  <mml:mi mathcolor=\"#000000\">P</mml:mi>\r\n" +
            "  <mml:mfenced>\r\n" +
            "    <mml:mrow>\r\n" +
            "      <mml:mi mathcolor=\"#000000\">A</mml:mi>\r\n" +
            "    </mml:mrow>\r\n" +
            "  </mml:mfenced>\r\n" +
            "  <mml:mo mathcolor=\"#000000\">\u2194</mml:mo>\r\n" +
            "  <mml:mi mathcolor=\"#000000\">x</mml:mi>\r\n" +
            "  <mml:mo mathcolor=\"#000000\">\u2286</mml:mo>\r\n" +
            "  <mml:mi mathcolor=\"#000000\">A</mml:mi>\r\n" +
            "</mml:math>";

        if(mathEditor2.ActualTheme == ElementTheme.Dark)
        {
            mathEditor2.Document.SetMathML(equationMathML.Replace("mathcolor=\"#000000\"", "mathcolor=\"#FFFFFF\""));
        }
        else
        {
            mathEditor2.Document.SetMathML(equationMathML.Replace("mathcolor=\"#FFFFFF\"", "mathcolor=\"#000000\""));
        }
    }
}
