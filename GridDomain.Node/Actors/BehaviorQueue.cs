using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GridDomain.Node.Actors {
    public class BehaviorQueue
    {
        private readonly Queue<string> _behaviorHistory;
        private readonly Action<Action> _become;
        private readonly int _historyLimit;
        public string Current { get; protected set; }
        public IReadOnlyCollection<string> History => _behaviorHistory;

        public BehaviorQueue(Action<Action> become, int historyLimit = 10)
        {
            _historyLimit = historyLimit;
            _behaviorHistory = new Queue<string>(historyLimit+1);
            _behaviorHistory.Enqueue("Default behavior");
            _become = become;
        }

        public virtual void Become(Action act, string name)
        {
            Current = name;
            Remember(name);

            _become(act);
        }

        protected void Remember(string name)
        {
            _behaviorHistory.Enqueue(name);
            if (_behaviorHistory.Count >= _historyLimit)
                _behaviorHistory.Dequeue();
        }
    }
}