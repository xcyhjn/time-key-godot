# Combat Shell Gate E performance review

> Status: bounded Player smoke passed; long-session profiling not claimed
> Source: `player-smoke-summary.json`

| Cycle | Transition ms | CPU frame ms | GPU frame ms | SetPass calls | Allocated memory bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 1674.3852 | 1.2661 | 0.149504 | 18 | 96305304 |
| 2 | 1512.9600 | 0.9814 | 0.147200 | 18 | 96660790 |
| 3 | 1513.1391 | 0.8675 | 0.146944 | 18 | 96717390 |

All cycles completed the typed Victory return with two loaded Scenes, one Bootstrap and one content entry. Total sampled memory growth was `412086` bytes. Raw samples are strictly monotonic, while `materiallyMonotonicMemoryGrowth=false` and `sustainedMonotonicMemoryGrowth=false`: the second delta is lower than the first instead of stable or increasing. The post-review targeted PlayMode test applies the same slope rule after GC and passed.

The Direct3D12 Development Player did not expose a valid positive `Draw Calls Count` or `Batches Count` recorder, so the smoke selected `SetPass Calls Count` and records each sample as `renderCounterValue`, not draw calls. That value and the frame samples are bounded smoke indicators only. A longer Unity Profiler capture remains necessary before making a production performance or leak claim.
