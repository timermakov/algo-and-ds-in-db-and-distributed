using Hw5.SearchIndex.Querying;
try { var n = SearchQueryParser.ParseQuery("device AND network"); Console.WriteLine(n.GetType().Name); }
catch (Exception e) { Console.WriteLine(e.Message); }
try { var n2 = SearchQueryParser.ParseQuery("american AND more"); Console.WriteLine(n2.GetType().Name); }
catch (Exception e) { Console.WriteLine(e.Message); }
