using Microsoft.UI.Xaml.Controls;
using Positioni.Core;

namespace Positioni;

public sealed partial class MainWindow
{
    private const int PercentUnitIndex = 0;
    private const int PixelUnitIndex = 1;

    private void SetDimension(NumberBox input, ComboBox unit, double value, bool percent)
    {
        input.Minimum = 1;
        input.Maximum = percent ? 100 : 32768;
        input.Minimum = percent ? 1 : input == WidthInput ? 100 : 80;
        input.Value = value;
        unit.SelectedIndex = percent ? PercentUnitIndex : PixelUnitIndex;
    }

    private void DimensionUnit_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (loading || selected is null || sender is not ComboBox unit || unit.SelectedIndex < 0) return;

        bool width = ReferenceEquals(unit, WidthUnit);
        var input = width ? WidthInput : HeightInput;
        bool percent = unit.SelectedIndex == PercentUnitIndex;
        try
        {
            double oldValue = DraftNumber(input);
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                loading = true;
                SetDimension(input, unit, double.NaN, percent);
                return;
            }
            var work = Native.WorkArea(hwnd, PrimaryInput.IsChecked == true);
            int extent = width ? work.Width : work.Height;
            if (!double.IsFinite(oldValue))
                oldValue = width ? (percent ? selected.Width : selected.WidthPercent ?? 50)
                                 : (percent ? selected.Height : selected.HeightPercent ?? 75);
            int value = percent
                ? (int)Math.Round(oldValue * 100 / extent, MidpointRounding.AwayFromZero)
                : Geometry.PercentToPixels((int)Math.Round(oldValue), extent);
            int minimum = percent ? 1 : width ? 100 : 80;
            loading = true;
            SetDimension(input, unit, Math.Clamp(value, minimum, percent ? 100 : 32768), percent);
        }
        catch (InvalidOperationException ex)
        {
            loading = true;
            unit.SelectedIndex = percent ? PixelUnitIndex : PercentUnitIndex;
            Notify(ex.Message, true);
        }
        finally
        {
            loading = false;
            UpdateProfileEditorState();
        }
    }
}
