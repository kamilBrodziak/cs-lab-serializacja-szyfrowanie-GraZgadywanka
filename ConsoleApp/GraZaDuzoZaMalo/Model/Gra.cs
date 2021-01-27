using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace GraZaDuzoZaMalo.Model {
    /// <summary>
    /// Klasa odpowiedzialna za logikę gry w "Za dużo za mało". Dostarcza API gry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 1. Gra może być w jednym z 3 możliwych statusów: 
    /// <list type="bullet">
    /// <item>
    /// <term><c>WTrakcie</c>
    /// </term>
    /// <description> - gracz jeszcze nie odgadł liczby, może podawać swoje propozycje, stan ustawiany w chwili utworzenia gry i może ulec zmianie jedynie w chwili odgadnięcia liczby lub jawnego przerwania gry,
    /// </description>
    /// </item> 
    /// <item>
    /// <term><c>Zakonczona</c></term>
    /// <description> - gracz odgadł liczbę, stan ustawiany wyłącznie w wyniku odgadnięcia liczby,</description>
    /// </item> 
    /// <item>
    /// <term><c>Poddana</c></term>
    /// <description>- gracz przerwał rozgrywkę, stan ustawiany wyłącznie w wyniku jawnego przerwania gry.</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// W chwili utworzenia obiektu gry losowana jest wartość do odgadnięcia, ustawiany czas rozpoczecia gry oraz gra otrzymuje status <c>WTrakcie</c>.
    /// </para>
    /// <para>
    /// Stan gry (w dowolnym momencie zycia obiektu gry) opisany jest przez:
    /// a) wylosowaną liczbę, którą należy odgadnąć,
    /// b) status gry (WTrakcie, Zakonczona, Poddana),
    /// c) historię ruchów graczagracz przerwał rozgrywke,  odgadującego (tzn. składane propozycje, czasy złożenia propozycji i odpowiedzi komputera).
    /// </para>
    /// <para>
    /// Komputer może udzielić jednej z 3 możliwych odpowiedzi: <c>ZaDuzo</c>, <c>ZaMalo</c>, <c>Trafiony</c>
    /// </para>
    /// <para>
    /// Pojedynczy Ruch
    /// </para>
    /// </remarks>
    [Serializable]
    [DataContract]
    public class Gra {
        /// <summary>
        /// Górne ograniczenie losowanej liczby, która ma zostać odgadnięta.
        /// </summary>
        /// <value>
        /// Domyślna wartość wynosi 100. Wartość jest ustawiana w konstruktorze i nie może zmienić się podczas życia obiektu gry.
        /// </value>
        public int MaxLiczbaDoOdgadniecia { get; } = 100;

        /// <summary>
        /// Dolne ograniczenie losowanej liczby, która ma zostać odgadnięta.
        /// </summary>
        /// <value>
        /// Domyślna wartość wynosi 1. Wartość jest ustawiana w konstruktorze i nie może zmienić się podczas życia obiektu gry.
        /// </value>
        public int MinLiczbaDoOdgadniecia { get; } = 1;
        [DataMember(Order = 14)]
        private byte[] liczbaDoOdgadniecia;
        [DataMember(Order = 15)]
        readonly private byte[] key;
        [DataMember (Order = 16)]
        readonly private byte[] IV;
        public int LiczbaDoOdgadniecia { get => DecryptNumber(liczbaDoOdgadniecia, key, IV); set { liczbaDoOdgadniecia = EncryptNumber(value, key, IV); } }

        /// <summary>
        /// Typ wyliczeniowy opisujący możliwe statusy gry.
        /// </summary>
        [DataContract]
        public enum Status {
            /// <summary>Status gry ustawiany w momencie utworzenia obiektu gry. Zmiana tego statusu mozliwa albo gdy liczba zostanie odgadnieta, albo jawnie przerwana przez gracza.</summary>
            [EnumMember]
            WTrakcie,
            /// <summary>Status gry ustawiany w momencie odgadnięcia poszukiwanej liczby.</summary>
            [EnumMember]
            Zakonczona,
            /// <summary>Status gry ustawiany w momencie jawnego przerwania gry przez gracza.</summary>
            [EnumMember]
            Poddana,
            [EnumMember]
            Zawieszona
        };

        /// <summary>
        /// Właściwość tylko do odczytu opisujaca aktualny status (<see cref="Status"/>) gry.
        /// </summary>
        /// <remarks>
        /// <para>W momencie utworzenia obiektu, uruchomienia konstruktora, zmienna przyjmuje wartość <see cref="Gra.Status.WTrakcie"/>.</para>
        /// <para>Zmiana wartości zmiennej na <see cref="Gra.Status.Poddana"/> po uruchomieniu metody <see cref="Przerwij"/>.</para>
        /// <para>Zmiana wartości zmiennej na <see cref="Gra.Status.Zakonczona"/> w metodzie <see cref="Propozycja(int)"/>, po podaniu poprawnej, odgadywanej liczby.</para>
        /// </remarks>
        [DataMember]
        public Status StatusGry { get; private set; }
        [DataMember]
        private List<Ruch> listaRuchow;
        public IReadOnlyList<Ruch> ListaRuchow { get { return listaRuchow.AsReadOnly(); } }

        /// <summary>
        /// Czas rozpoczęcia gry, ustawiany w momencie utworzenia obiektu gry, w konstruktorze. Nie można go już zmodyfikować podczas życia obiektu.
        /// </summary>
        [DataMember]
        public DateTime CzasRozpoczecia { get; private set; } // musiałem dodać setter, gdyż pole nie może być readonly przy serializacji
        public DateTime? CzasZakonczenia { get; private set; }

        /// <summary>
        /// Zwraca aktualny stan gry, od chwili jej utworzenia (wywołania konstruktora) do momentu wywołania tej własciwości.
        /// </summary>
        public TimeSpan AktualnyCzasGry => DateTime.Now - CzasRozpoczecia;
        public TimeSpan CalkowityCzasGry => (StatusGry == Status.WTrakcie) ? AktualnyCzasGry : (TimeSpan)(CzasZakonczenia - CzasRozpoczecia);

        public Gra(int min, int max) {
            if(min >= max)
                throw new ArgumentException();

            MinLiczbaDoOdgadniecia = min;
            MaxLiczbaDoOdgadniecia = max;
            using (Aes aes = Aes.Create()) {
                key = aes.Key;
                IV = aes.IV;
                LiczbaDoOdgadniecia = (new Random()).Next(MinLiczbaDoOdgadniecia, MaxLiczbaDoOdgadniecia + 1);
            }
            CzasRozpoczecia = DateTime.Now;
            CzasZakonczenia = null;
            StatusGry = Status.WTrakcie;

            listaRuchow = new List<Ruch>();
        }

        static byte[] EncryptNumber(int num, byte[] Key, byte[] IV) {
            byte[] encrypted;
            using(Aes aes = Aes.Create()) {
                aes.Key = Key;
                aes.IV = IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using(MemoryStream msEncrypt = new MemoryStream()) {
                    using(CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using(StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
                            swEncrypt.Write(num);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        static int DecryptNumber(byte[] bytes, byte[] Key, byte[] IV) {
            int plaintext;
            using(Aes aesAlg = Aes.Create()) {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using(MemoryStream msDecrypt = new MemoryStream(bytes)) {
                    using(CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using(StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                            plaintext = int.Parse(srDecrypt.ReadToEnd());
                        }
                    }
                }
            }
            return plaintext;
        }

        public Gra() : this(1, 100) { }


        /// <summary>
        /// Każde zadanie pytania o wynik skutkuje dopisaniem do listy
        /// </summary>
        /// <param name="pytanie"></param>
        /// <returns></returns>
        public Odpowiedz Ocena(int pytanie) {
            Odpowiedz odp;
            if(pytanie == LiczbaDoOdgadniecia) {
                odp = Odpowiedz.Trafiony;
                StatusGry = Status.Zakonczona;
                CzasZakonczenia = DateTime.Now;
                listaRuchow.Add(new Ruch(pytanie, odp, Status.Zakonczona));
            } else if(pytanie < LiczbaDoOdgadniecia)
                odp = Odpowiedz.ZaMalo;
            else
                odp = Odpowiedz.ZaDuzo;

            //dopisz do listy
            if(StatusGry == Status.WTrakcie) {
                listaRuchow.Add(new Ruch(pytanie, odp, Status.WTrakcie));
            }

            return odp;
        }

        public void Przerwij() {
            if(StatusGry == Status.WTrakcie) {
                StatusGry = Status.Zawieszona;
                CzasZakonczenia = DateTime.Now;
                listaRuchow.Add(new Ruch(null, null, Status.Zawieszona));
            }
        }

        public void Wznow() {
            if(StatusGry == Status.Zawieszona) {
                StatusGry = Status.WTrakcie;
            }
        }

        public int Poddaj() {
            if(StatusGry == Status.WTrakcie) {
                StatusGry = Status.Poddana;
                CzasZakonczenia = DateTime.Now;
                listaRuchow.Add(new Ruch(null, null, Status.Poddana));
            }
            return LiczbaDoOdgadniecia;
        }

        // struktury wewnętrzne, pomocnicze
        [DataContract]
        public enum Odpowiedz {
            [EnumMember]
            ZaMalo = -1,
            [EnumMember]
            Trafiony = 0,
            [EnumMember]
            ZaDuzo = 1
        };

        [Serializable]
        [DataContract]
        public class Ruch {
            [DataMember]
            public int? Liczba { get; private set; }
            [DataMember]
            public Odpowiedz? Wynik { get; private set; }
            [DataMember]
            public Status StatusGry { get; private set; }
            [DataMember]
            public DateTime Czas { get; private set; }

            public Ruch(int? propozycja, Odpowiedz? odp, Status statusGry) {
                this.Liczba = propozycja;
                this.Wynik = odp;
                this.StatusGry = statusGry;
                this.Czas = DateTime.Now;
            }

            public override string ToString() {
                return $"({Liczba}, {Wynik}, {Czas}, {StatusGry})";
            }
        }


    }
}
