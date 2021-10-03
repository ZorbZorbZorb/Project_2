using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts {
    [Serializable]
    public class GameData {
        public static void ExportData(string path, GameData data) {
            throw new NotImplementedException();
        }
        public static GameData ImportData(string path) {
            throw new NotImplementedException();
        }

        public int night;
        public int wettings;
        public double funds;

        public int bathroomToilets;
        public int bathroomUrinals;
        public int bathroomSinks;

        public bool barHasBar;
        public int barStools;
        public int barSeats;

        public GameData() {
            barHasBar = true;
            barStools = 6;
            barSeats = -1;
            bathroomSinks = -1;
            bathroomToilets = -1;
            bathroomUrinals = -1;
            night = 1;
            wettings = 0;
            funds = 0d;
        }
    }
}
