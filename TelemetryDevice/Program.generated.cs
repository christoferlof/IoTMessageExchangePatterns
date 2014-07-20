//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TelemetryDevice {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The Joystick module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Joystick joystick;
        
        /// <summary>The UsbClientDP module using socket 1 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.UsbClientDP usbClientDP;
        
        /// <summary>The Button module using socket 4 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Button calibrateButton;
        
        /// <summary>The Ethernet_J11D (Premium) module using socket 7 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Ethernet_J11D ethernet_J11D;
        
        /// <summary>The MulticolorLed module using socket 11 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.MulticolorLed multicolorLed;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZSpider Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZSpider)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZSpider();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.joystick = new GTM.GHIElectronics.Joystick(9);
            this.usbClientDP = new GTM.GHIElectronics.UsbClientDP(1);
            this.calibrateButton = new GTM.GHIElectronics.Button(4);
            this.ethernet_J11D = new GTM.GHIElectronics.Ethernet_J11D(7);
            this.multicolorLed = new GTM.GHIElectronics.MulticolorLed(11);
        }
    }
}
