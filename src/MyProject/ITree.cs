namespace MyProject;

public partial interface ITree 
{
    ITree Self();
}

public partial interface ICs : ITree 
{
    
}

public partial interface IChild : ICs 
{
     
}

public class Test : ICs<Test>
{
    public Test Self()
    {
        throw new NotImplementedException();
    }
}