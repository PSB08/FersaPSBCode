using System.Collections.Generic;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicValidationReport
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();
        
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool HasError => _errors.Count > 0;
        public bool HasWarning => _warnings.Count > 0;
        
        public void AddError(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _errors.Add(message);
        }
        
        public void AddWarning(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _warnings.Add(message);
        }
        
    }
}
