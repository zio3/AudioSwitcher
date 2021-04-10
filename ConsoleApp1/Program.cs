using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Diagnostics;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static readonly Guid GUID_AT2020P = new Guid("e60020c5-ca92-4e8a-8353-505099b2b498");
        static readonly Guid GUID_SPEKER = new Guid("8e285cf8-e6be-49c1-b64c-baaa4dad1b57");

        static readonly string REALID_AT2020P = "{0.0.0.00000000}.{e60020c5-ca92-4e8a-8353-505099b2b498}";
        static readonly string REALID_SPEKER = "{0.0.0.00000000}.{8e285cf8-e6be-49c1-b64c-baaa4dad1b57}";

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            //Console.Beep();

            ////Console.WriteLine("Hello World!");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"{sw.Elapsed}");
            var ac = new CoreAudioController();
            ////var devices = ac.GetDevices().ToList();

            ////var activeDevices = devices.Where(c => c.State == AudioSwitcher.AudioApi.DeviceState.Active).ToArray();


            ////ac.GetDevice()

            ////var item = activeDevices.First(c => c.InterfaceName.Contains("AC511"));
            ////item.IsDefaultDevice
            ///
            var ddId = ac.GetDefaultDeviceId(DeviceType.Playback,Role.Communications);

            //var id = ac.DefaultPlaybackDevice.Id;


            //var nextDevceGuid = id == GUID_AT2020P ?
            //        GUID_SPEKER : GUID_AT2020P;

            var nextDevceRealId = ddId == REALID_AT2020P ?
                    REALID_SPEKER : REALID_AT2020P;




            Console.WriteLine($"{sw.Elapsed}");
            //var device = ac.GetDevice(nextDevceGuid);
            //Console.WriteLine($"{sw.Elapsed}");
            //device.SetAsDefault();

            PolicyConfig.SetDefaultEndpoint(nextDevceRealId, ERole.Console | ERole.Communications);
            PolicyConfig.SetDefaultEndpoint(nextDevceRealId, ERole.Console | ERole.Multimedia);

            Console.WriteLine($"{sw.Elapsed}");
            //new CoreAudioDevice(item, ac);
            //await ac.DefaultPlaybackDevice.SetAs(false);

//            item.SetAsDefault();

        }
    }
}
