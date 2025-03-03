Generator:Generate() →    {
                    Fetch('Data');
                    Process('Data');
                }
                
NewAge:Generator.Generate ← 
{

    On(Fetch) →
    {   
        ReadExports('Data');
        ReadCallbacks('Data');
    }
    
    ReadExports('Data') :=
    {
        this.DpReadExports(this.GetData('Data'));
    }

    ReadCallbacks('Data') :=
    {
        this.DoReadCallbacks(this.GetData('Data'));
    }
    
    On(Process) →
    {
        Each('Export' in 'Data'[ReadExports])  → ProcessExport('Export', 'Information');
    
        //DoProcess(Data, Information);
    }
    
    After(Process) →
    {
        DoWriteOutput('Information')
    }
    
    On(ProcessExport) →
    {
        Export ∧ ThisReference →
        {
            Information:Classes ← Ensure(Create)(Export as ThisReference)
        }
        
        //DoProcessExport(Export, Information)}
    }
    
    
    // in this simple example image DoProcess and DoWriteOutput to be defined in the NewAge::Generator '.cs'. But I want to move as much as I can into here, 
}