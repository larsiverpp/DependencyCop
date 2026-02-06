using UsingNamespaceStatementAnalyzer.Account;

namespace UsingNamespaceStatementAnalyzer.Account
{
    class Id
    {
        public string GetValue() => "IdValue";
    }
}

namespace UsingNamespaceStatementAnalyzer.Transaction
{
    class Current
    {
        private readonly Id _accountId;

        public Current(Id accountId)
        {
            _accountId = accountId;
        }

        public string GetFancyValue() => _accountId.GetValue() + " - FancyValue";
    }
}
