using GraZaDuzoZaMalo;
using GraZaDuzoZaMalo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using static GraZaDuzoZaMalo.Model.Gra.Odpowiedz;

namespace AppGraZaDuzoZaMaloCLI {
    public class KontrolerCLI {
        private Gra gra;
        private WidokCLI widok;

        public int MinZakres { get; private set; } = 1;
        public int MaxZakres { get; private set; } = 100;

        public IReadOnlyList<Gra.Ruch> ListaRuchow {
            get { return gra.ListaRuchow; }
        }

        public TimeSpan AktualnyCzasGry => gra.AktualnyCzasGry;
        public DateTime CzasRozpoczecia => gra.CzasRozpoczecia;

        public KontrolerCLI() {
            widok = new WidokCLI(this);
        }

        public void Uruchom() {
            widok.OpisGry();
            while(widok.ChceszKontynuowac("Czy chcesz kontynuować aplikację (t/n)? "))
                UruchomRozgrywke();
        }

        private void ZapiszGreCo10Sekund() {
            while(gra.StatusGry == Gra.Status.WTrakcie) {
                //BinarySerialization.SerializeToFile<Gra>(gra);
                DataContractSerialization.SerializeToFile<Gra>(gra);
                for(int i = 0; i < 100; i++) {
                    if(gra.StatusGry != Gra.Status.WTrakcie) break;
                    Thread.Sleep(100);
                }
            }
        }

        public void UruchomRozgrywke() {
            widok.CzyscEkran();
            WczytajGre();
            var t = new Thread(new ThreadStart(ZapiszGreCo10Sekund));
            t.Start();
            do {
                //wczytaj propozycję
                int propozycja = 0;
                try {
                    propozycja = widok.WczytajPropozycje();
                    widok.CzyscEkran();
                } catch(ZawieszenieGryException) {
                    gra.Przerwij();
                } catch(KoniecGryException) {
                    gra.Poddaj();
                }

                if(gra.StatusGry == Gra.Status.Poddana || gra.StatusGry == Gra.Status.Zawieszona)
                    break;
                
                Console.WriteLine(propozycja);
                switch(gra.Ocena(propozycja)) {
                    case ZaDuzo:
                        widok.KomunikatZaDuzo();
                        break;
                    case ZaMalo:
                        widok.KomunikatZaMalo();
                        break;
                    case Trafiony:
                        widok.KomunikatTrafiono();
                        break;
                    default:
                        break;
                }
                widok.HistoriaGry();
            }
            while(gra.StatusGry == Gra.Status.WTrakcie);
            t.Join();
            ZakonczGre();
        }

        public int LiczbaProb() => gra.ListaRuchow.Count();
        private void WczytajGre() {
            bool newGame = true;
            //if(BinarySerialization.SaveExists()) {
            if(DataContractSerialization.SaveExists()) {
                try {
                    gra = DataContractSerialization.DeserializeFromFile<Gra>();
                    //gra = BinarySerialization.DeserializeFromFile<Gra>();
                    widok.HistoriaGry();
                    if(widok.ChceszKontynuowac("Istnieje zapis Twojej gry z podanymi statystykami, czy chcesz go wczytać (t/n)? ")) {
                        newGame = false;
                        gra.Wznow();
                    }
                    DataContractSerialization.DeleteSave();
                    //BinarySerialization.DeleteSave();
                } catch(SaveException e) {
                    widok.Wypisz(e.Message);
                }
            }
            if(newGame) {
                gra = new Gra(MinZakres, MaxZakres);
            }
        }

        public void ZakonczGre() {
            try {
                //BinarySerialization.SerializeToFile<Gra>(gra);
                DataContractSerialization.SerializeToFile<Gra>(gra);
            } catch(SaveException e) {
                gra.Poddaj();
                Console.WriteLine(e.Message);
            }
            if(gra.StatusGry == Gra.Status.Poddana) {
                widok.Wypisz($"Poprawna odpowiedź to: {gra.LiczbaDoOdgadniecia}");
            }
            if(gra.StatusGry == Gra.Status.Poddana || gra.StatusGry == Gra.Status.Zakonczona) {
                // BinarySerialization.DeleteSave();
                widok.Wypisz($"Liczba prób: {LiczbaProb()}");
                DataContractSerialization.DeleteSave();
            }
            gra = null;
        }

        public void ZakonczRozgrywke() {
            gra.Przerwij();
        }
    }

    [Serializable]
    internal class KoniecGryException : Exception {
        public KoniecGryException() {
        }

        public KoniecGryException(string message) : base(message) {
        }

        public KoniecGryException(string message, Exception innerException) : base(message, innerException) {
        }

        protected KoniecGryException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
    internal class ZawieszenieGryException : Exception {
        public ZawieszenieGryException() {
        }

        public ZawieszenieGryException(string message) : base(message) {
        }

        public ZawieszenieGryException(string message, Exception innerException) : base(message, innerException) {
        }

        protected ZawieszenieGryException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
