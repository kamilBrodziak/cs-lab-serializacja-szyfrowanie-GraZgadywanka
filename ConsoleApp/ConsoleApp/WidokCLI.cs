using System;
using static System.Console;

namespace AppGraZaDuzoZaMaloCLI {
    class WidokCLI {
        public const char ZNAK_ZAWIESZENIA_GRY = 'X', ZNAK_PODDANIA_GRY = 'C';

        private KontrolerCLI kontroler;

        public WidokCLI(KontrolerCLI kontroler) => this.kontroler = kontroler;

        public void CzyscEkran() => Clear();

        public void KomunikatPowitalny() => WriteLine("Wylosowałem liczbę z zakresu ");

        public int WczytajPropozycje() {
            int wynik = 0;
            bool sukces = false;
            while(!sukces) {
                Write("Podaj swoją propozycję (lub " + ZNAK_ZAWIESZENIA_GRY + " aby przerwać lub " + ZNAK_PODDANIA_GRY + " aby poddać ): ");
                try {
                    string value = ReadLine().TrimStart().ToUpper();
                    if(value.Length > 0 && value[0].Equals(ZNAK_ZAWIESZENIA_GRY))
                        throw new ZawieszenieGryException();
                    if(value.Length > 0 && value[0].Equals(ZNAK_PODDANIA_GRY))
                        throw new KoniecGryException();
                    //UWAGA: ponizej może zostać zgłoszony wyjątek 
                    wynik = Int32.Parse(value);
                    sukces = true;
                } catch(FormatException) {
                    WriteLine("Podana przez Ciebie wartość nie przypomina liczby! Spróbuj raz jeszcze.");
                    continue;
                } catch(OverflowException) {
                    WriteLine("Przesadziłeś. Podana przez Ciebie wartość jest zła! Spróbuj raz jeszcze.");
                    continue;
                } catch(ZawieszenieGryException) {
                    throw new ZawieszenieGryException();
                } catch(KoniecGryException) {
                    throw new KoniecGryException();
                } catch(Exception) {
                    WriteLine("Nieznany błąd! Spróbuj raz jeszcze.");
                    continue;
                }
            }
            return wynik;
        }

        public void OpisGry() {
            WriteLine("Gra w \"Za dużo za mało\"." + Environment.NewLine
                + "Twoimm zadaniem jest odgadnąć liczbę, którą wylosował komputer." + Environment.NewLine + "Na twoje propozycje komputer odpowiada: za dużo, za mało albo trafiłeś");
        }

        public bool ChceszKontynuowac(string prompt) {
            Write(prompt);
            char odp = ReadKey().KeyChar;
            WriteLine();
            return (odp == 't' || odp == 'T');
        }

        public void Wypisz(string tekst) {
            Console.WriteLine(tekst);
        }

        public void HistoriaGry() {
            if(kontroler.ListaRuchow.Count == 0) {
                WriteLine("--- pusto ---");
                return;
            }

            WriteLine("Nr    Propozycja     Odpowiedź     Czas    Status");
            WriteLine("=================================================");
            int i = 1;
            double calkowityCzasZawieszenia = 0;
            DateTime czasZawieszenia = DateTime.Now;
            bool czyPoprzedniRuchBylZawieszeniem = false;
            foreach(var ruch in kontroler.ListaRuchow) {
                if(ruch.StatusGry == GraZaDuzoZaMalo.Model.Gra.Status.Zawieszona) {
                    czasZawieszenia = ruch.Czas;
                    czyPoprzedniRuchBylZawieszeniem = true;
                } else if(czyPoprzedniRuchBylZawieszeniem) {
                    calkowityCzasZawieszenia += (ruch.Czas - czasZawieszenia).TotalSeconds;
                    czyPoprzedniRuchBylZawieszeniem = false;
                }
                WriteLine($"{i}     {ruch.Liczba}      {ruch.Wynik}  {(ruch.Czas - kontroler.CzasRozpoczecia).TotalSeconds - calkowityCzasZawieszenia}   {ruch.StatusGry}");
                i++;
            }
        }

        public void KomunikatZaDuzo() {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Za dużo!");
            Console.ResetColor();
        }

        public void KomunikatZaMalo() {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Za mało!");
            Console.ResetColor();
        }

        public void KomunikatTrafiono() {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine("Trafiono!");
            Console.ResetColor();
        }
    }

}
