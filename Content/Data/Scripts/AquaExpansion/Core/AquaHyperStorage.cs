using Sandbox.ModAPI;
using System;
using System.IO;

namespace AquaExpansion.Core
{
    public static class AquaHyperStorage
    {

        public static void AquaHyperSave(AquaWaterSettings settings, string FileName)
        {
            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(FileName);
                if (writer != null)
                {
                    string xml = MyAPIGateway.Utilities.SerializeToXML<AquaWaterSettings>(settings);
                    writer.Write(xml);
                    writer.Close();
                }
            }
            catch (Exception exept)
            {
                AquaExpansionSession.Insance.Log(true, $"Error saving: {exept.Message}");
            }
        }

        public static AquaWaterSettings AquaHyperLoad(string FileName)
        {
            AquaWaterSettings Settings = new AquaWaterSettings();
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInGlobalStorage(FileName))
                {
                    using (TextReader reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(FileName))
                    {
                        if (reader != null)
                        {
                            string xml = reader.ReadToEnd();

                            if (!string.IsNullOrWhiteSpace(xml))
                            {
                                var loaded = MyAPIGateway.Utilities.SerializeFromXML<AquaWaterSettings>(xml);
                                if (!loaded.Equals(default(AquaWaterSettings)))
                                    Settings = loaded;
                            }
                        }
                    }
                }
            }
            catch (Exception exept)
            {
                AquaExpansionSession.Insance.Log(true, $"Error loading: {exept.Message}");
            }
            return Settings;
        }
    }
}
