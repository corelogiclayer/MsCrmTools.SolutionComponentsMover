namespace HeadlessSolutionComponentsMover
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            /*url, tenantId, clientId, clientSecret, targetSolution, publisherId*/
            new Importer(args[0], args[1], args[2], args[3]).Import(null, args[4], Guid.Parse(args[5]));
        }

       

    }
}
