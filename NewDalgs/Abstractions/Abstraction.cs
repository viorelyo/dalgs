namespace NewDalgs.Abstractions
{
    abstract class Abstraction
    {
        protected string _abstractionId;
        protected System.System _system;

        protected Abstraction(string abstractionId, System.System system)
        {
            _abstractionId = abstractionId;
            _system = system;
        }

        public abstract bool Handle(ProtoComm.Message msg);

        public string GetId()
        {
            return _abstractionId;
        }
    }
}
