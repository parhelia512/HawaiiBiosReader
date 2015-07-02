﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace HawaiBiosReader
{
    public class GridRow
    {
        private String _position;
        private int _value;
        private String  _unit;

        public GridRow(String pos,int val,String un)
        {
            _position = pos;
            _value = val;
            _unit = un;
        }
        public String position
        {
            get { return _position; }
            set { _position = value; }
        }

        public int value
        {
            get { return _value; }
            set { _value = value; }
        }

        public String unit 
        {
            get { return _unit; }
            set { _unit = value; }
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<GridRow> data = new ObservableCollection<GridRow>();
        ObservableCollection<GridRow> list = new ObservableCollection<GridRow>();
        Byte[] romStorageBuffer; // whole rom
        Byte[] powerTablepattern = new Byte[] { 0x02, 0x06, 0x01, 0x00 };
        Byte[] voltageObjectInfoPattern = new Byte[] { 0x08,0x96,0x60,0x00};
        Byte[] FanControlpattern = new byte[] { 0x07, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        Byte[] FanControl2pattern = new byte[] { 0x03, 0x06, 0x7C, 0x15 }; // pattern to search for in buffer
        Byte[] FanControl3pattern = new byte[] { 0x07, 0x06, 0x68, 0x10 }; // pattern to search for in buffer

        int powerTablePosition; // start position of powertable in rom
        int voltageInfoPosition;
        int fanTablePosition;
        int powerTableSize;
        int voltageInfoSize;

        int fanTableOffset = 175;
        int biosNameOffset = 0xDC;
        int tdpLimitOffset = 630;
        int tdcLimitOffset = 632;
        int powerDeliveryLimitOffset = 642;

        // table offsets
        int voltageTableOffset = 319; // 290 have different voltagetable offset than 390
        int memoryFrequencyTableOffset = 278;
        int gpuFrequencyTableOffset = 231;
        int VCELimitTableOffset = 396;
        int AMUAndACPLimitTableOffset = 549;
        int UVDLimitTableOffset = 441;
        string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();


        public MainWindow()
        {
            InitializeComponent();
            versionbox.Text += version;
        }

        private void bOpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog.Filter = "Bios files (.rom)|*.rom|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog.OpenFile();
                filename.Text = openFileDialog.FileName;

                using (BinaryReader br = new BinaryReader(fileStream)) // binary reader
                {
                    romStorageBuffer = br.ReadBytes((int)fileStream.Length);
                    powerTablePosition = PTPatternAt(romStorageBuffer, powerTablepattern);
                    voltageInfoPosition = PatternAt(romStorageBuffer, voltageObjectInfoPattern);
                    fanTablePosition = PatternAt(romStorageBuffer, FanControlpattern);
                    if (fanTablePosition == -1)
                    {
                        fanTablePosition = PatternAt(romStorageBuffer, FanControl2pattern);
                        if(fanTablePosition == -1)
                        {
                            fanTablePosition = PatternAt(romStorageBuffer, FanControl3pattern);
                        }

                    }

                    biosName.Text = getTextFromBinary(romStorageBuffer, biosNameOffset, 32);
                    gpuID.Text = romStorageBuffer[565].ToString("X2") + romStorageBuffer[564].ToString("X2") + "-" + romStorageBuffer[567].ToString("X2") + romStorageBuffer[566].ToString("X2"); // not finished working only for few bioses :(

                    if (powerTablePosition == -1)
                    {
                        MessageBoxResult result = MessageBox.Show("PowerTable position not found in this file", "Error", MessageBoxButton.OK);
                    }
                    else
                    {
                        powerTableSize = 256 * romStorageBuffer[powerTablePosition + 1] + romStorageBuffer[powerTablePosition];
                        powerTablesize.Text = powerTableSize.ToString();


                        /*#################################################################################################
                         * 
                         *               BIOS PARSING SECTION
                         * 
                        #################################################################################################*/
                        switch (powerTableSize)
                        {
                            case 660:
                                powerTablesize.Text += " - R9 390/390X";
                                voltageTableOffset = 319;
                                memoryFrequencyTableOffset = 278;
                                gpuFrequencyTableOffset = 231;
                                VCELimitTableOffset = 521;
                                AMUAndACPLimitTableOffset = 549;
                                UVDLimitTableOffset = 439;
                                tdpLimitOffset = 632;
                                tdcLimitOffset = 634;
                                powerDeliveryLimitOffset = 644;
                                break;
                            case 662:
                                powerTablesize.Text += " - R9 390/390X(Sapphire)";
                                voltageTableOffset = 321;
                                memoryFrequencyTableOffset = 280;
                                gpuFrequencyTableOffset = 233;
                                VCELimitTableOffset = 523;
                                AMUAndACPLimitTableOffset = 551;
                                UVDLimitTableOffset = 441;
                                tdpLimitOffset = 634;
                                tdcLimitOffset = 636;
                                powerDeliveryLimitOffset = 646;
                                break;
                            case 650:
                                powerTablesize.Text += " - R9 290X MSI Lightning";
                                voltageTableOffset = 309;
                                memoryFrequencyTableOffset = 268;
                                gpuFrequencyTableOffset = 221;
                                VCELimitTableOffset = 511;
                                AMUAndACPLimitTableOffset = 539;
                                UVDLimitTableOffset = 429;
                                tdpLimitOffset = 622;
                                tdcLimitOffset = 624;
                                powerDeliveryLimitOffset = 634;
                                break;
                            case 648:
                                powerTablesize.Text += " - R9 290/290X";
                                voltageTableOffset = 307;
                                memoryFrequencyTableOffset = 266;
                                gpuFrequencyTableOffset = 219;
                                VCELimitTableOffset = 509;
                                AMUAndACPLimitTableOffset = 537;
                                UVDLimitTableOffset = 427;
                                tdpLimitOffset = 620;
                                tdcLimitOffset = 622;
                                powerDeliveryLimitOffset = 632;
                                break;
                            case 658: // Slith mining bios for 290/290X
                                powerTablesize.Text += " - R9 290/290X The Stilt mining bios";
                                voltageTableOffset = 316;
                                memoryFrequencyTableOffset = 275;
                                gpuFrequencyTableOffset = 228;
                                VCELimitTableOffset = 519;
                                AMUAndACPLimitTableOffset = 547;
                                UVDLimitTableOffset = 437;
                                tdpLimitOffset = 630;
                                tdcLimitOffset = 632;
                                powerDeliveryLimitOffset = 642;
                                break;
                            case 642: // PT1/PT3
                                powerTablesize.Text += " - PT1/PT3 bios";
                                voltageTableOffset = 300;
                                memoryFrequencyTableOffset = 259;
                                gpuFrequencyTableOffset = 212;
                                VCELimitTableOffset = 503;
                                AMUAndACPLimitTableOffset = 531;
                                UVDLimitTableOffset = 421;
                                tdpLimitOffset = 614;
                                tdcLimitOffset = 616;
                                powerDeliveryLimitOffset = 626;
                                break;
                            case 634: // FirePro W9100
                                powerTablesize.Text += " - FirePro W9100";
                                voltageTableOffset = 317;
                                memoryFrequencyTableOffset = 276;
                                gpuFrequencyTableOffset = 229;
                                VCELimitTableOffset = 495;
                                AMUAndACPLimitTableOffset = 523;
                                UVDLimitTableOffset = 425;
                                tdpLimitOffset = 606;
                                tdcLimitOffset = 608;
                                powerDeliveryLimitOffset = 618;
                                break;
                            default:
                                powerTablesize.Text = powerTablesize.Text + " - Unknown type";
                                break;

                        }

                        powerTablePositionValue.Text = "0x" + powerTablePosition.ToString("X");
                        powerTable.Text = getTextFromBinary(romStorageBuffer, powerTablePosition, powerTableSize);

                        int position = 0;
                        // gpu clock1
                        readValueFromPosition(gpuclock1, powerTablePosition + 98, 1, "Mhz", true);
                        // gpu clock 2
                        readValueFromPosition(gpuclock2, powerTablePosition + 107, 1, "Mhz", true);
                        // gpu clock 3
                        readValueFromPosition(gpuclock3, powerTablePosition + 116, 1, "Mhz", true);

                        // mem clock 1
                        readValueFromPosition(memclock1, powerTablePosition + 101, 1, "Mhz", true);
                        // mem clock 2
                        readValueFromPosition(memclock2, powerTablePosition + 110, 1, "Mhz", true);
                        // mem clock 3
                        readValueFromPosition(memclock3, powerTablePosition + 119, 1, "Mhz", true);


                        readValueFromPosition(tdpLimit, powerTablePosition + tdpLimitOffset, 0, "W");
                        readValueFromPosition(powerLimit, powerTablePosition + powerDeliveryLimitOffset, 0, "W");
                        readValueFromPosition(tdcLimit,powerTablePosition + tdcLimitOffset,0,"A");


                        // read voltage table
                        voltagetable.Text = "";
                        for (int i = 0; i < 24; i++)
                        {
                            readValueFromPosition(voltagetable, powerTablePosition + voltageTableOffset + (i * 2), 0, "mV" + System.Environment.NewLine, false,true);
                            
                            list.Add(new GridRow("0x" + (powerTablePosition + voltageTableOffset + (i * 2)).ToString("X"), get16BitValueFromPosition(powerTablePosition + voltageTableOffset + (i * 2),romStorageBuffer,false),"mV"));
                            
                        }
                        voltageEdit.ItemsSource = list;
                        // memory frequency table
                        memfrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPosition(memfrequencytable, powerTablePosition + memoryFrequencyTableOffset + (i * 5), 1, "Mhz" + System.Environment.NewLine, true, true);
                        }

                        // gpu frequency table
                        gpufrequencytable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            readValueFromPosition(gpufrequencytable, powerTablePosition + gpuFrequencyTableOffset + (i * 5), 1, "Mhz" + System.Environment.NewLine, true, true);
                        }
                        // search for more 24 bit
                        limitValues.Text = "";
                        for (int i = 0; i < 10; i++)
                        {
                            readValueFromPosition(limitValues, powerTablePosition + AMUAndACPLimitTableOffset + 81+ (i * 3), 1, "" + System.Environment.NewLine, false, true);
                        }

                        // search for more 16 bit
                        limitValues2.Text = "";
                        for (int i = 0; i < 16; i++)
                        {

                            readValueFromPosition(limitValues2, powerTablePosition + AMUAndACPLimitTableOffset + 79 + (i * 2), 0, "" + System.Environment.NewLine, false, true);
                        }
                        // search for more 24 bit

                        if (voltageInfoPosition > 0)
                        {
                            voltageinfo.Text = "";
                            for (int i = 0; i < 20; i++)
                            {
                                readValueFromPosition(voltageinfo, powerTablePosition + voltageInfoPosition + 1 + (i * 3), 1, "" + System.Environment.NewLine, false, true);
                            }

                            // search for more 16 bit
                            voltageinfo2.Text = "";
                            for (int i = 0; i < 32; i++)
                            {
                                readValueFromPosition(voltageinfo2, powerTablePosition + voltageInfoPosition + 1 + (i * 2), 0, "" + System.Environment.NewLine, false, true);
                            }
                        }
                        // StartVCELimitTable
                        VCELimitTableValues.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + VCELimitTableOffset + (i * 3);
                            readValueFromPosition(VCELimitTableValues, position, 0, "--", false, true);
                            VCELimitTableValues.Text += romStorageBuffer[position + 2] + System.Environment.NewLine;
                        }

                        // StartUVDLimitTable
                        UVDLimitTable.Text = "";
                        for (int i = 0; i < 8; i++)
                        {
                            position = powerTablePosition + UVDLimitTableOffset + (i * 3);
                            readValueFromPosition(UVDLimitTable, position, 0, "--", false, true);
                            UVDLimitTable.Text += romStorageBuffer[position + 2] + System.Environment.NewLine;
                        }

                        // StartSAMULimitTable + StartACPLimitTable
                        AMULimitTable.Text = "";
                        ACPLimitTable.Text = "";
                        for (int i = 0; i < 16; i++)
                        {
                            if (i <= 7)
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + (i * 5);
                                AMULimitTable.Text += "0x" + position.ToString("X") + "  -- ";
                                AMULimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                AMULimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                            else
                            {
                                position = powerTablePosition + AMUAndACPLimitTableOffset + 2 + (i * 5);
                                ACPLimitTable.Text += "0x" + position.ToString("X") + "  -- ";
                                ACPLimitTable.Text += get16BitValueFromPosition(position - 2, romStorageBuffer) + "  -- ";
                                ACPLimitTable.Text += get24BitValueFromPosition(position, romStorageBuffer) + System.Environment.NewLine;
                            }
                        }


                        if (fanTablePosition > 0)
                        {
                            readValueFromPosition(temperatureHysteresis, fanTablePosition + 1, 2, "C°");
                            readValueFromPosition(fantemperature1, fanTablePosition + 2, 0, "C°",true);
                            readValueFromPosition(fantemperature2, fanTablePosition + 4, 0, "C°",true);
                            readValueFromPosition(fantemperature3, fanTablePosition + 6, 0, "C°",true);
                            readValueFromPosition(fantemperature4, fanTablePosition + 14, 0, "C°",true);

                            readValueFromPosition(fanspeed1, fanTablePosition + 8, 0, "%",true);
                            readValueFromPosition(fanspeed2, fanTablePosition + 10, 0, "%",true);
                            readValueFromPosition(fanspeed3, fanTablePosition + 12, 0, "%",true);
                            readValueFromPosition(fanControlType, fanTablePosition + 16, 2, "", true);
                            readValueFromPosition(pwmFanMax, fanTablePosition + 17, 2, "%");
                            readValueFromPosition(maxAsicTemperature, fanTablePosition + 459, 2, "C°");
                            // 
                            readValueFromPosition(gpuMaxClock, fanTablePosition + 33, 1, "Mhz");// this offset work only for 390X need some polishing for other cards
                            readValueFromPosition(memMaxClock, fanTablePosition + 37, 1, "Mhz");
                        }
                        else
                        {
                            temperatureHysteresis.Text = "NOT FOUND";
                            fanControlType.Text = "NOT FOUND";
                            pwmFanMax.Text = "NOT FOUND";
                            maxAsicTemperature.Text = "NOT FOUND";
                            fanspeed1.Text = "NOT FOUND";
                            fanspeed2.Text = "NOT FOUND";
                            fanspeed3.Text = "NOT FOUND";
                            fantemperature1.Text = "NOT FOUND";
                            fantemperature2.Text = "NOT FOUND";
                            fantemperature3.Text = "NOT FOUND";
                            fantemperature4.Text = "NOT FOUND";
                        }
                    }
                    fileStream.Close();
                }
            }
        }
        public void readValueFromPosition(TextBox dest, int position, int type, String units = "", bool isFrequency = false, bool add = false)
        {
            if (add)
            {
                dest.Text += "0x" + position.ToString("X") + " -- ";
            }
            else
            {
                dest.Text = "0x" + position.ToString("X") + " -- ";
            }

            switch (type)
            {
                case 0: // 16 bit value
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 1: // 24 bit value
                    dest.Text += get24BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
                case 2: // 8 bit value
                    dest.Text += romStorageBuffer[position].ToString() + " " + units;
                    break;
                default:
                    dest.Text += get16BitValueFromPosition(position, romStorageBuffer, isFrequency).ToString() + " " + units;
                    break;
            }
        }
        private static int PTPatternAt(byte[] data, byte[] pattern)
        {
            for (int di = 0; di < data.Length; di++)		
                if(data[di] == pattern[0] && data[di + 1] == pattern[1] && data[di + 2] == pattern[2] && data[di + 3] == pattern[3])
                {
                    return di - 1;
                }
            return -1;
        }

        private static int PatternAt(byte[] data, byte[] pattern)
        {
            for (int i = 0; i < data.Length; )
            {
                int j;
                for (j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != data[i])
                        break;
                    i++;
                }
                if (j == pattern.Length)
                {
                    return i - pattern.Length;
                }
                if (j != 0) continue;
                i++;
            }
            return -1;
        }
        public String getTextFromBinary(byte[] binary, int offset, int lenght)
        {
            System.Text.Encoding encEncoder = System.Text.ASCIIEncoding.ASCII;
            string str = encEncoder.GetString(binary.Skip(offset).Take(lenght).ToArray());
            return str;
        }
        // dumb way to extract 24 bit value (can be made much more effective but this is easy to read for anyone)
        public Int32 get24BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if(position < buffer.Length - 1)
            {
            if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
            {
                return (256 * 256 * buffer[position + 2] + 256 * buffer[position + 1] + buffer[position]) / 100;
            }
            return 256 * 256 * buffer[position + 2] + 256 * buffer[position + 1] + buffer[position];
            }
            return -1;
        }

        public Int32 get16BitValueFromPosition(int position, byte[] buffer, bool isFrequency = false)
        {
            if (position < buffer.Length - 1)
            {
                if (isFrequency) // if its frequency divide by 100 to convert it into Mhz
                {
                    return (256 * buffer[position + 1] + buffer[position]) / 100;
                }
                return 256 * buffer[position + 1] + buffer[position];
            }
            return -1;
        }

        private void voltageEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            voltageEdit.Columns[0].IsReadOnly = true;
            voltageEdit.Columns[1].IsReadOnly = false;
            voltageEdit.Columns[2].IsReadOnly = true;
        }

    }
}
