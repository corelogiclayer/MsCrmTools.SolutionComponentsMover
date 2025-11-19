using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using MsCrmTools.SolutionComponentsMover.AppCode;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeadlessSolutionComponentsMover
{
    internal class Importer
    {
        private EntityMetadataCollection _emc;
        private OptionMetadataCollection _omc;
        private List<Entity> _solutionComponents;
        private SolutionManager sManager;
        private IOrganizationService service;
        private IEnumerable<Entity> solutions;


        public Importer(string url, string tenantId, string clientId, string clientSecret)
        {
            service = ConnectToD365(url, tenantId, clientId, clientSecret);
        }

        public void Import(string solutionSelectionRegex, string targetSolution, Guid? publisherId = null)
        {
            LoadSolutions();

            List<Entity> selectedSolutions;

            if (publisherId.HasValue == false)
            {
                selectedSolutions = solutions.Where(s => Regex.IsMatch(s["uniquename"]?.ToString(), solutionSelectionRegex)).ToList();
            }
            else
            {
                selectedSolutions = solutions.Where(s => ((EntityReference)s["publisherid"]).Id.Equals(publisherId)).ToList();
            }
            

            var targetSolutionRec = solutions.FirstOrDefault(s => s["uniquename"]?.ToString() == targetSolution);
            
            selectedSolutions.Remove(targetSolutionRec);

            if (targetSolutionRec == default)
            {
                throw new Exception("targetsolution not found!");
            }

            CopyComponents(selectedSolutions, targetSolutionRec, new List<int>(), true);
        }


        private IOrganizationService ConnectToD365(string url, string tenantId, string clientId, string clientSecret)
        {
            string conString =
                $@"AuthType=ClientSecret;
                   Url={url};
                   ClientId={clientId};
                   ClientSecret={clientSecret};
                   TenantId={tenantId};";

            var serviceClient = new ServiceClient(conString);

            return serviceClient;
        }

        public void LoadSolutions(int orgMajorVersion = 9, int orgMinorVersion = 1)
        {
            sManager = new SolutionManager(service);
            solutions = sManager.RetrieveSolutions().ToList();

            _omc = ((OptionSetMetadata)((RetrieveOptionSetResponse)service.Execute(
                new RetrieveOptionSetRequest
                {
                    Name = "componenttype"
                })).OptionSetMetadata).Options;

            _emc = MetadataHelper.LoadEntities(service);

            if (orgMajorVersion >= 9 && orgMinorVersion >= 1)
            {
                _solutionComponents = service.RetrieveMultiple(new QueryExpression("solutioncomponentdefinition")
                {
                    NoLock = true,
                    ColumnSet = new ColumnSet("objecttypecode", "primaryentityname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                            {
                                new ConditionExpression("canbeaddedtosolutioncomponents", ConditionOperator.Equal, true)
                            }
                    }
                }).Entities.ToList();
            }
        }

        private void CopyComponents(List<Entity> sourceSolutions, Entity targetSolution, List<int> componentTypes, bool allComponents, bool checkForbestPractice = false, int orgMajorVersion = 9, int orgMinorVersion = 1)
        {
            var settings = new CopySettings
            {
                SourceSolutions = sourceSolutions,
                TargetSolutions = new List<Entity>() { targetSolution },
                ConnectionDetail = new ConnectionDetail()
                {
                    UseOnline = true
                },
                CheckBestPractice = checkForbestPractice,
                ComponentsTypes = componentTypes,
                AllComponents = allComponents
            };

            sManager.CopyComponents(settings, _omc, _emc, _solutionComponents, settings.ConnectionDetail.UseOnline);
        }
    }
}
