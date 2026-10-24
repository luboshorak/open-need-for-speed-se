using System;

namespace OpenNFS1
{
    /// <summary>
    /// Detects crossings of an end-of-lap or end-of-stage road node.
    /// Driving can skip several TrackNodes per frame, so equality comparisons
    /// on node.Number are not reliable.
    /// </summary>
    internal sealed class RaceGateTracker
    {
        private readonly int _finish;
        private readonly int _halfway;
        private readonly bool _openRoad;
        private int _previous;
        private bool _passedHalfway;

        public RaceGateTracker(int startingNode, int finishNode, bool openRoad)
        {
            if (finishNode < 2)
                throw new ArgumentOutOfRangeException(nameof(finishNode));
            _previous = startingNode;
            _finish = finishNode;
            _halfway = Math.Max(1, finishNode / 2);
            _openRoad = openRoad;
        }

        /// <summary>Return true once when driving forward past the finish gate.</summary>
        public bool Update(int currentNode)
        {
            int previousNode = _previous;
            _previous = currentNode;

            // A closed circuit wraps from the final road nodes to node zero.
            // At high speeds the frame can jump over the finish node AND the
            // wrap; detect that crossing without treating ordinary reversing
            // as a finish. Open roads never wrap.
            if (!_openRoad && currentNode < previousNode)
            {
                int window = Math.Max(8, Math.Min(40, _finish / 20));
                if (_passedHalfway &&
                    previousNode >= _finish - window &&
                    currentNode <= window)
                {
                    _passedHalfway = false;
                    return true;
                }
                return false;
            }

            if (currentNode == previousNode)
                return false;

            // Circuits must pass a halfway checkpoint before an actual lap
            // counts, disallowing a quick reverse/forward shortcut at the line.
            if (!_openRoad && previousNode < _halfway && currentNode >= _halfway)
                _passedHalfway = true;

            bool crossedFinish = previousNode < _finish && currentNode >= _finish;
            if (!crossedFinish || (!_openRoad && !_passedHalfway))
                return false;

            _passedHalfway = false;
            return true;
        }
    }
}
