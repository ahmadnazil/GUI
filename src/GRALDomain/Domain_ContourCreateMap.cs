#region Copyright
///<remarks>
/// <GRAL Graphical User Interface GUI>
/// Copyright (C) [2019]  [Dietmar Oettl, Markus Kuntner]
/// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
/// the Free Software Foundation version 3 of the License
/// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
/// You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
///</remarks>
#endregion

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace GralDomain
{
    public partial class Domain
    {
        /// <summary>
        /// OpenFileDialog for contour maps and read map data from file
        /// </summary>
        private void CreateContourMap(string file)
        {
            CultureInfo ic = CultureInfo.InvariantCulture;

            if (file.Length < 1)
            {
                using (OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "(*.dat;*.txt)|*.dat;*.txt",
                    Title = "Select raster data (ASCII Format)",
                    InitialDirectory = Path.Combine(Gral.Main.ProjectName, "Maps" + Path.DirectorySeparatorChar),
                    ShowHelp = true
#if NET6_0_OR_GREATER
                    ,ClientGuid = GralStaticFunctions.St_F.FileDialogMaps
#endif
                })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        file = dialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (File.Exists(file))
            {
                try
                {
                    Cursor = Cursors.WaitCursor;

                    // Read ESRI file
                    GralIO.ReadESRIFile readESRIFile = new GralIO.ReadESRIFile();
#if NET6_0_OR_GREATER
                    (double[,] zlevel, GralIO.ESRIHeader header, double min, double max, string exception) = readESRIFile.ReadESRIFileMultiDim(file);
#else
                    double min = 0, max = 0;
                    string exception = string.Empty;
                    GralIO.ESRIHeader header = new GralIO.ESRIHeader();
                    double[,] zlevel = readESRIFile.ReadESRIFileMultiDim(file, ref header, ref min, ref max, ref exception);
#endif
                    if (zlevel == null)
                    {
                        throw new IOException(exception);
                    }

                    if (Math.Abs(max - min) < 0.000000000001 && !Path.GetFileName(file).Contains("PrognosticSubDomainAreas.txt"))
                    {
                        MessageBox.Show(this, "Blank raster dataset", "Process raster data", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                        Cursor = Cursors.Default;
                        return;
                    }

                    //add contour map to object list
                    const int cellsizemax = 200000;

                    DrawingObjects _drobj = new DrawingObjects("CM: " + Path.GetFileNameWithoutExtension(file));

                    if (Path.GetFileName(file) == "building_heights.txt" || Path.GetFileName(file).Contains("steady_state.txt")
                        || Path.GetFileName(file).Contains("TPI_STDI.txt")
                        || Path.GetFileName(file).Contains("TPI_SlopeMax.txt")
                        || Path.GetFileName(file).Contains("TPI_SlopeMin.txt")
                        || Path.GetFileName(file).Contains("TPI_Base.txt")
                        || Path.GetFileName(file).Contains("PrognosticSubDomainAreas.txt")
                        || (header.NCols * header.NRows) > cellsizemax) // set special settings for buildings, steady state files and large contour maps
                    {
                        _drobj.FillYesNo = true;
                    }
                    else
                    {
                        _drobj.FillYesNo = false;
                    }

                    if (Path.GetFileName(file) == "building_heights.txt") // set special settings for buildings
                    {
                        SetBuildingsStyle(_drobj);
                    }
                    else if (Path.GetFileName(file).Contains("TPI_STDI.txt")) // set special settings for TPI Maps
                    {
                        SetTPIStyle(_drobj);
                    }
                    else if (Path.GetFileName(file).Contains("steady_state.txt")) // set special settings for GRAMM steady state files
                    {
                        SetSteadyStateStyle(_drobj);
                    }
                    else if (Path.GetFileName(file).Contains("PrognosticSubDomainAreas.txt")) // special settings for SubDomainAreas
                    {
                        SetSubDomainStyle(_drobj);
                    }
                    else if (Path.GetFileName(file).Contains("RoughnessLengthsGral.txt")) // special settings for RoughnessLengths files
                    {
                        SetRoughnessLenghtStyle(_drobj);
                    }
                    else
                    {
                        //compute values for 9 contours
                        for (int i = 0; i < 9; i++)
                        {
                            _drobj.ItemValues.Add(0);
                            _drobj.FillColors.Add(Color.Red);
                            _drobj.LineColors.Add(Color.Red);
                        }

                        _drobj.FillColors[0] = Color.Yellow;
                        _drobj.LineColors[0] = Color.Yellow;

                        // initial scale of contour map
                        _drobj.ItemValues[0] = min + (max - min) / Math.Pow(2, Convert.ToDouble(8));
                        _drobj.ItemValues[8] = max;

                        //apply color gradient between light green and red
                        int r1 = _drobj.FillColors[0].R;
                        int g1 = _drobj.FillColors[0].G;
                        int b1 = _drobj.FillColors[0].B;
                        int r2 = _drobj.FillColors[8].R;
                        int g2 = _drobj.FillColors[8].G;
                        int b2 = _drobj.FillColors[8].B;
                        for (int i = 0; i < 7; i++)
                        {
                            _drobj.ItemValues[i + 1] = min + (max - min) / Math.Pow(2, Convert.ToDouble(8 - (i + 1)));
                            int intr = r1 + (r2 - r1) / 10 * (i + 1);
                            int intg = g1 + (g2 - g1) / 10 * (i + 1);
                            int intb = b1 + (b2 - b1) / 10 * (i + 1);
                            _drobj.FillColors[i + 1] = Color.FromArgb(intr, intg, intb);
                            _drobj.LineColors[i + 1] = Color.FromArgb(intr, intg, intb);
                        }

                        if (header.VerticalConcentrationMap)
                        {
                            _drobj.LegendTitle = "Vertical Concentration";
                        }
                        else
                        {
                            _drobj.LegendTitle = "Title";
                        }

                        if (header.Unit.Length == 0) // unit not available from file -> loot to filename
                        {
                            string temp = Path.GetFileName(file).ToUpper();
                            if (temp.Contains("ODOUR"))
                            {
                                _drobj.LegendUnit = "%";
                            }
                            else if (temp.Contains("WINDSPEED") && temp.Contains("TXT"))
                            {
                                _drobj.LegendUnit = "m/s";
                            }
                            else if (temp.Contains("DEPOSITION"))
                            {
                                _drobj.LegendUnit = Gral.Main.mg_p_m2;
                            }
                            else if (temp.Contains("TPI_Base.txt"))
                            {
                                _drobj.LegendUnit = "m";
                            }
                            else if (temp.Contains("TPI_SlopeMin.txt") || temp.Contains("TPI_SlopeMax.txt"))
                            {
                                _drobj.LegendUnit = "°";
                            }
                            else
                            {
                                _drobj.LegendUnit = Gral.Main.My_p_m3;
                            }
                        }
                        else // unit read from file
                        {
                            _drobj.LegendUnit = header.Unit;
                        }


                        if (Path.GetFileName(file).Contains("roughness.txt")
                            || header.VerticalConcentrationMap // set special settings for GRAMM roughness lenght
                            || Path.GetFileName(file).Contains("TPI_Base.txt"))
                        {
                            _drobj.Filter = false;
                        }
                        else // set filter on by default
                        {
                            _drobj.Filter = true;
                        }

                        if ((header.NCols * header.NRows) <= cellsizemax) // few points -> show contour lines
                        {
                            _drobj.LineWidth = 1;
                            _drobj.ColorScale = "-999,-999,-999";
                        }
                        else // large area -> don't show lines!
                        {
                            _drobj.LineWidth = 0;
                            _drobj.ColorScale = Convert.ToString(picturebox1.Width - 150) + "," + Convert.ToString(picturebox1.Height - 200) + "," + "1";
                        }
                    }
                    //
                    //add list to save contourpoints
                    //
                    _drobj.ContourFilename = file;
                    ItemOptions.Insert(0, _drobj);
                    SaveDomainSettings(1);

                    //compute contour polygons
                    ReDrawContours = true;
                    Contours(file, _drobj);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Process raster data", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
                Cursor = Cursors.Default;
            }

        }

        /// <summary>
        /// Set drawing style for Sub Domain Areas
        /// </summary>
        /// <param name="_drobj">Drawing Object</param>
        private static void SetSubDomainStyle(DrawingObjects _drobj)
        {
            //compute values for 2 contours
            for (int i = 0; i < 2; i++)
            {
                _drobj.ItemValues.Add(0.1);
                _drobj.FillColors.Add(Color.Red);
                _drobj.LineColors.Add(Color.Red);
            }
            _drobj.ItemValues[1] = 1;
            _drobj.FillColors[0] = Color.Aqua;
            _drobj.LineColors[0] = Color.Blue;
            _drobj.LegendTitle = "GRAL Sub Domain Areas";
            _drobj.LegendUnit = " ";
            _drobj.Filter = false; // no filter
            _drobj.LineWidth = 0; // no Lines
            _drobj.Transparancy = 160;
        }
        /// <summary>
        /// Set drawing style for Roughness Lenghts files
        /// </summary>
        /// <param name="_drobj">Drawing Object</param>
        private static void SetRoughnessLenghtStyle(DrawingObjects _drobj)
        {
            //compute values for 8 contours
            for (int i = 0; i < 8; i++)
            {
                _drobj.ItemValues.Add(0);
                _drobj.FillColors.Add(Color.DarkGray);
                _drobj.LineColors.Add(Color.DarkGray);
            }
            // initial scale of contour map
            for (int i = 0; i < 8; i++)
            {
                _drobj.ItemValues[i] = (i + 1) * 0.2;
                Color c = Color.FromArgb(255 - i * 32, 255 - i * 32, 255 - i * 32);
                _drobj.FillColors[i] = c;
                _drobj.LineColors[i] = c;
            }
            _drobj.LegendTitle = "GRAL Roughness Lenghts";
            _drobj.LegendUnit = "m";
            _drobj.Filter = false; // no filter
            _drobj.LineWidth = 0; // no Lines
            _drobj.Transparancy = 160;
            _drobj.FillYesNo = true;
        }
        /// <summary>
        /// Set drawing style for GRAMM Steady State Files
        /// </summary>
        /// <param name="_drobj">Drawing Object</param>
        private static void SetSteadyStateStyle(DrawingObjects _drobj)
        {
            //compute values for 8 contours
            for (int i = 0; i < 8; i++)
            {
                _drobj.ItemValues.Add(0);
                _drobj.FillColors.Add(Color.Red);
                _drobj.LineColors.Add(Color.Red);
            }
            _drobj.ItemValues[0] = -0.1;
            _drobj.FillColors[0] = Color.Red;
            _drobj.LineColors[0] = Color.Red;

            // initial scale of contour map
            for (int i = 1; i < 8; i++)
            {
                _drobj.ItemValues[i] = Math.Round(i - 0.1, 1);
                Color c = Color.FromArgb(0, i * 32, 255 - i * 32);
                _drobj.FillColors[i] = c;
                _drobj.LineColors[i] = c;
            }

            _drobj.LegendTitle = "SteadyStateError";
            _drobj.LegendUnit = "er";
            _drobj.Filter = false; // no filter
            _drobj.LineWidth = 0; // no Lines
        }
        /// <summary>
        /// Set drawing style for TPI Files
        /// </summary>
        /// <param name="_drobj">Drawing Object</param>
        private static void SetTPIStyle(DrawingObjects _drobj)
        {
            //compute values for 10 contours
            for (int i = 0; i < 9; i++)
            {
                _drobj.ItemValues.Add(0);
                _drobj.FillColors.Add(Color.Red);
                _drobj.LineColors.Add(Color.Red);
            }
            _drobj.FillColors[0] = Color.Yellow;
            _drobj.LineColors[0] = Color.Yellow;
            _drobj.FillYesNo = true;

            // initial scale of contour map
            for (int i = 0; i < 9; i++)
            {
                _drobj.ItemValues[i] = i;
                Color c = Color.FromArgb(159, 19, 19);
                if (i == 0)
                {
                    c = Color.FromArgb(0, 255, 255);
                }
                else if (i == 1)
                {
                    c = Color.FromArgb(0, 180, 180);
                }
                else if (i == 2)
                {
                    c = Color.FromArgb(128, 128, 255);
                }
                else if (i == 3)
                {
                    c = Color.FromArgb(128, 200, 255);
                    c = Color.FromArgb(16, 16, 16);
                }
                else if (i == 4)
                {
                    c = Color.FromArgb(255, 255, 0);
                    c = Color.FromArgb(32, 32, 32);
                }
                else if (i == 5)
                {
                    c = Color.FromArgb(255, 128, 64);
                }
                else if (i == 6)
                {
                    c = Color.FromArgb(208, 67, 0);
                }
                else if (i == 7)
                {
                    c = Color.FromArgb(235, 93, 93);
                }
                else if (i == 8)
                {
                    c = Color.FromArgb(219, 26, 26);
                }

                _drobj.FillColors[i] = c;
                _drobj.LineColors[i] = c;
            }

            _drobj.LegendTitle = "TPI Map";
            _drobj.LegendUnit = "TPI";
            _drobj.Filter = false; // no filter
            _drobj.LineWidth = 0; // no Lines
        }
        /// <summary>
        /// Set drawing style for Building files
        /// </summary>
        /// <param name="_drobj">Drawing Object</param>
        private static void SetBuildingsStyle(DrawingObjects _drobj)
        {
            //compute values for 6 contours
            for (int i = 0; i < 6; i++)
            {
                _drobj.ItemValues.Add(0);
                _drobj.FillColors.Add(Color.Red);
                _drobj.LineColors.Add(Color.Red);
            }
            _drobj.FillColors[0] = Color.Yellow;
            _drobj.LineColors[0] = Color.Yellow;

            // initial scale of contour map
            for (int i = 0; i < 6; i++)
            {
                _drobj.ItemValues[i] = i * 3 + 3;
                Color c = Color.FromArgb(255 - (i + 1) * 33, 255 - (i + 1) * 33, 255 - (i + 1) * 12);
                _drobj.FillColors[i] = c;
                _drobj.LineColors[i] = c;
            }

            _drobj.LegendTitle = "Buildings";
            _drobj.LegendUnit = "m";
            _drobj.Filter = false; // no filter
            _drobj.LineWidth = 0; // no Lines
        }
    }
}
