using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIA;

namespace ScanServerYozhegCmd
{
    public class Scanner
    {
        string AppDataFolder = Environment.GetEnvironmentVariable("appdata") + "\\ScannerApp1";
        public string Config = "";
        private Device _scanDevice;
        private Item _scannerItem;
        private Random _rnd = new Random();
        private Dictionary<string, object> _defaultDeviceProp;

        //##########################################################################################################################

        public Scanner()
        {
            if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
            Config = AppDataFolder + "\\scanner.cfg";
            try
            {
                LoadConfig();
            }
            catch (Exception ex)
            {
                log.Add(ex.ToString());
                Console.WriteLine("Помилка в налаштуваннях, треба ручна настройка сканера");
                Configuration();
            }
        }

        public void Configuration()
        {
            try
            {
                var commonDialog = new WIA.CommonDialog();
                _scanDevice = commonDialog.ShowSelectDevice(WiaDeviceType.ScannerDeviceType, true);
                if (_scanDevice == null)
                    return;
                var items = commonDialog.ShowSelectItems(_scanDevice);
                if (items.Count < 1)
                    return;
                _scannerItem = items[1];
                SaveProp(_scanDevice.Properties, ref _defaultDeviceProp);
                SaveConfig();
            }
            catch (Exception ex)
            {
                log.Add(ex.ToString());
                Console.WriteLine(ex.Message, "Інтерфейс сканера не доступний");
            }
        }

        private void SaveProp(WIA.Properties props, ref Dictionary<string, object> dic)
        {
            if (dic == null) dic = new Dictionary<string, object>();

            foreach (Property property in props)
            {
                var propId = property.PropertyID.ToString();
                var propValue = property.get_Value();

                dic[propId] = propValue;
            }
        }

        public void SetDuplexMode(bool isDuplex)
        {
            // WIA property ID constants
            const string wiaDpsDocumentHandlingSelect = "3088";
            const string wiaDpsPages = "3096";

            // WIA_DPS_DOCUMENT_HANDLING_SELECT flags
            const int feeder = 0x001;
            const int duplex = 0x004;

            if (_scanDevice == null) return;

            if (isDuplex)
            {
                SetProp(_scanDevice.Properties, wiaDpsDocumentHandlingSelect, (duplex | feeder));
                SetProp(_scanDevice.Properties, wiaDpsPages, 1);
            }
            else
            {
                try
                {
                    SetProp(_scanDevice.Properties, wiaDpsDocumentHandlingSelect, _defaultDeviceProp[wiaDpsDocumentHandlingSelect]);
                    SetProp(_scanDevice.Properties, wiaDpsPages, _defaultDeviceProp[wiaDpsPages]);
                }
                catch (Exception ex)
                {
                    log.Add(ex.ToString());
                    Console.WriteLine(String.Format("Збій відновлення режиму сканування:{0}{1}", Environment.NewLine, ex.Message));
                }
            }
        }

        public MemoryStream MemScan()
        {
            if ((_scannerItem == null))
            {
                log.Add("Сканер не налаштований!");
                Console.WriteLine("Сканер не налаштований!", "Info");
                return null;

            }

            var stream = new MemoryStream();

            try
            {
                //var result = _scannerItem.Transfer(FormatID.wiaFormatJPEG);
                var result = _scannerItem.Transfer();
                var wiaImage = (ImageFile)result;
                var imageBytes = (byte[])wiaImage.FileData.get_BinaryData();

                using (var ms = new MemoryStream(imageBytes))
                {
                    using (var bitmap = Bitmap.FromStream(ms))
                    {
                        bitmap.Save(stream, ImageFormat.Jpeg);
                    }
                }

            }
            catch (Exception ex)
            {
                log.Add(ex.ToString());
                return null;
            }

            return stream;
        }

        private void SaveConfig()
        {
            var settings = new List<string>();
            settings.Add("[device]");
            settings.Add(String.Format("DeviceID;{0}", _scanDevice.DeviceID));

            foreach (IProperty property in _scanDevice.Properties)
            {
                var propstring = string.Format("{1}{0}{2}{0}{3}", ";", property.Name, property.PropertyID, property.get_Value());
                settings.Add(propstring);
            }

            settings.Add("[item]");
            settings.Add(String.Format("ItemID;{0}", _scannerItem.ItemID));
            foreach (IProperty property in _scannerItem.Properties)
            {
                var propstring = string.Format("{1}{0}{2}{0}{3}", ";", property.Name, property.PropertyID, property.get_Value());
                settings.Add(propstring);
            }

            File.WriteAllLines(Config, settings.ToArray());
        }

        private enum loadMode { undef, device, item };

        private void LoadConfig()
        {
            var settings = File.ReadAllLines(Config);

            var mode = loadMode.undef;

            foreach (var setting in settings)
            {
                if (setting.StartsWith("[device]"))
                {
                    mode = loadMode.device;
                    continue;
                }

                if (setting.StartsWith("[item]"))
                {
                    mode = loadMode.item;
                    continue;
                }

                if (setting.StartsWith("DeviceID"))
                {
                    var deviceid = setting.Split(';')[1];
                    var devMngr = new WIA.DeviceManager();

                    foreach (IDeviceInfo deviceInfo in devMngr.DeviceInfos)
                    {
                        if (deviceInfo.DeviceID == deviceid)
                        {
                            _scanDevice = deviceInfo.Connect();
                            break;
                        }
                    }

                    if (_scanDevice == null)
                    {
                        Console.WriteLine("Сканнер из конигурации не найден");
                        return;
                    }

                    _scannerItem = _scanDevice.Items[1];
                    continue;
                }

                if (setting.StartsWith("ItemID"))
                {
                    var itemid = setting.Split(';')[1];
                    continue;
                }

                var sett = setting.Split(';');
                switch (mode)
                {
                    case loadMode.device:
                        SetProp(_scanDevice.Properties, sett[1], sett[2]);
                        break;

                    case loadMode.item:
                        SetProp(_scannerItem.Properties, sett[1], sett[2]);
                        break;
                }
            }
            SaveProp(_scanDevice.Properties, ref _defaultDeviceProp);
        }

        private static void SetProp(IProperties prop, object property, object value)
        {
            try
            {
                prop[property].set_Value(value);
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
