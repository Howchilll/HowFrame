using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace HowFrame
{
    /// <summary>
    /// 通用 Job 系统（支持 Burst + UpdateHelper 驱动）
    /// </summary>
    public delegate void JobComputeDelegate<TJobData, TItem>(ref TJobData jobData, int index, ref TItem item)
        where TJobData : struct
        where TItem : struct;

    public class JobHelper<T, P, J> : IDisposable
        where T : struct
        where J : struct
    {
        private readonly UpdateHelper _updater;
        private readonly JobComputeDelegate<J, T> _onCompute;
        private readonly Action<NativeArray<T>, List<P>, List<T>> _onApply;
        public bool Enable { get; set; } = true;
        
        private NativeArray<T> _bufferA;
        private NativeArray<T> _bufferB;

        private readonly List<T> _dataList = new();
        private readonly List<P> _objs = new();

        private bool _hasNewResult = false;
        private readonly object _sync = new();
        private readonly object _dataLock = new();

        private JobHandle _currentJobHandle;
        private bool _isJobRunning = false;
        private NativeArray<T> _currentSnapshot;

        private J _jobData;

        public JobHelper(
            JobComputeDelegate<J, T> computeCallback,
            Action<NativeArray<T>, List<P>, List<T>> applyCallback,
            int fps = 60)
        {
            _onCompute = computeCallback;
            _onApply = applyCallback;

            _updater = new UpdateHelper(fps, true);
            _updater.OnUpdate += OnFrame;
        }

        public void SetJobData(J data) => _jobData = data;

        public void AddData(P obj, T data)
        {
            lock (_dataLock)
            {
                _objs.Add(obj);
                _dataList.Add(data);
            }
        }

        public void RemoveData(P obj)
        {
            lock (_dataLock)
            {
                int index = _objs.IndexOf(obj);
                if (index >= 0)
                {
                    _objs.RemoveAt(index);
                    _dataList.RemoveAt(index);
                }
            }
        }

        private void OnFrame()
        {
            if (!Enable) return;
            int count;
            lock (_dataLock)
                count = _dataList.Count;

            if (count == 0)
                return;

            // --- 初始化缓冲区 ---
            if (!_bufferA.IsCreated || _bufferA.Length < count)
            {
                if (_bufferA.IsCreated) _bufferA.Dispose();
                if (_bufferB.IsCreated) _bufferB.Dispose();

                _bufferA = new NativeArray<T>(count, Allocator.Persistent);
                _bufferB = new NativeArray<T>(count, Allocator.Persistent);
            }

            // --- 检查上一个 Job 是否完成 ---
            if (_isJobRunning && _currentJobHandle.IsCompleted)
            {
                _currentJobHandle.Complete();
                _isJobRunning = false;

                if (_currentSnapshot.IsCreated)
                    _currentSnapshot.Dispose();

                lock (_sync)
                {
                    (_bufferA, _bufferB) = (_bufferB, _bufferA);
                    _hasNewResult = true;
                }
            }

            // --- 新 Job 调度 ---
            if (!_isJobRunning)
            {
                NativeArray<T> snapshot;
                lock (_dataLock)
                {
                    snapshot = new NativeArray<T>(_dataList.Count, Allocator.TempJob);
                    for (int i = 0; i < _dataList.Count; i++)
                        snapshot[i] = _dataList[i];
                }

                _bufferA.CopyFrom(snapshot);
                _currentSnapshot = snapshot;

                var job = new InternalJob<T, J>
                {
                    snapshot = snapshot,
                    writeBuffer = _bufferA,
                    jobData = _jobData,
                    compute = _onCompute
                };

                int batchSize = Math.Max(1, count / Environment.ProcessorCount);
                _currentJobHandle = job.Schedule(count, batchSize);
                _isJobRunning = true;
            }

            // --- 应用结果 ---
            if (_hasNewResult)
            {
                NativeArray<T> readBuffer;
                lock (_sync)
                {
                    readBuffer = _bufferB;
                    _hasNewResult = false;
                }

                lock (_dataLock)
                {
                    _onApply?.Invoke(readBuffer, _objs, _dataList);
                }
            }
        }

        public void Dispose()
        {
            _updater.OnUpdate -= OnFrame;
            _updater.Dispose();

            if (_bufferA.IsCreated) _bufferA.Dispose();
            if (_bufferB.IsCreated) _bufferB.Dispose();
            if (_currentSnapshot.IsCreated) _currentSnapshot.Dispose();
        }

        // === 内部自动托管 Job ===
        [BurstCompile]
        private struct InternalJob<TT, JJ> : IJobParallelFor
            where TT : struct
            where JJ : struct
        {
            [ReadOnly] public NativeArray<TT> snapshot;
            [WriteOnly] public NativeArray<TT> writeBuffer;
            public JJ jobData;
            public JobComputeDelegate<JJ, TT> compute;

            public void Execute(int index)
            {
                var val = snapshot[index];
                compute(ref jobData, index, ref val);
                writeBuffer[index] = val;
            }
        }
    }
}
