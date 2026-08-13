namespace Reporting.Application.Models
{
    // The uniform shape every ingestion command returns — deliberately
    // NOT the read model's own DTO, since "did this get applied or was
    // it already processed" is what a delivery/dispatcher-driven caller
    // actually needs to know, the same reasoning ApplySaleResultDto
    // (Warehouse, C3) already established for the identical idempotent-
    // receiver situation.
    public class IngestResultDto
    {
        public bool AlreadyProcessed { get; set; }
    }
}
