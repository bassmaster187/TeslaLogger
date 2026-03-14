# SQL Optimization Testing & Performance Validation
## Raspberry Pi 3B Performance Measurement Framework

---

## TABLE OF CONTENTS
1. [Baseline Performance Measurement](#baseline)
2. [Testing Strategy](#testing)
3. [Performance Benchmarking Tools](#tools)
4. [Before/After Comparison](#comparison)
5. [Monitoring & Alerting](#monitoring)
6. [Troubleshooting Guide](#troubleshooting)

---

## <a name="baseline"></a>1. BASELINE PERFORMANCE MEASUREMENT

### Step 1: Capture Current Performance BEFORE Optimizations

**Create baseline performance report script**:

```csharp
// File: PerformanceBaseline.cs
using System;
using System.Diagnostics;
using System.Collections.Generic;

public class PerformanceBaseline
{
    private List<PerformanceMetric> metrics = new List<PerformanceMetric>();
    
    public struct PerformanceMetric
    {
        public string Operation { get; set; }
        public long ExecutionTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
        public int RowsAffected { get; set; }
        public double MemoryBefore_MB { get; set; }
        public double MemoryAfter_MB { get; set; }
    }
    
    public void MeasureOperationPerformance(string operationName, Action operation)
    {
        var process = Process.GetCurrentProcess();
        double memBefore = process.WorkingSet64 / (1024.0 * 1024.0);
        
        var sw = Stopwatch.StartNew();
        operation.Invoke();
        sw.Stop();
        
        double memAfter = process.WorkingSet64 / (1024.0 * 1024.0);
        
        metrics.Add(new PerformanceMetric
        {
            Operation = operationName,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Timestamp = DateTime.Now,
            MemoryBefore_MB = memBefore,
            MemoryAfter_MB = memAfter
        });
        
        Console.WriteLine($"[PERF] {operationName}: {sw.ElapsedMilliseconds}ms, Memory: {memBefore:F1}MB→{memAfter:F1}MB");
    }
    
    public void GenerateBaselineReport(string filename)
    {
        using (var writer = new System.IO.StreamWriter(filename))
        {
            writer.WriteLine("TESLALOGGER PERFORMANCE BASELINE REPORT");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Host: {Environment.MachineName}");
            writer.WriteLine($"OS: {Environment.OSVersion}");
            writer.WriteLine();
            
            writer.WriteLine("Operation | Time (ms) | Memory Before (MB) | Memory After (MB) | Memory Delta (MB)");
            writer.WriteLine("-----------|-----------|-------------------|-------------------|-----------------");
            
            foreach (var metric in metrics)
            {
                double memDelta = metric.MemoryAfter_MB - metric.MemoryBefore_MB;
                writer.WriteLine($"{metric.Operation} | {metric.ExecutionTimeMs} | {metric.MemoryBefore_MB:F1} | {metric.MemoryAfter_MB:F1} | {memDelta:F1}");
            }
            
            writer.WriteLine();
            writer.WriteLine("Summary Statistics:");
            writer.WriteLine($"Total Operations Measured: {metrics.Count}");
            writer.WriteLine($"Average Execution Time: {metrics.Average(m => m.ExecutionTimeMs):F0}ms");
            writer.WriteLine($"Max Execution Time: {metrics.Max(m => m.ExecutionTimeMs)}ms");
            writer.WriteLine($"Min Execution Time: {metrics.Min(m => m.ExecutionTimeMs)}ms");
            
            double avgMemDelta = 0;
            foreach (var m in metrics)
                avgMemDelta += (m.MemoryAfter_MB - m.MemoryBefore_MB);
            writer.WriteLine($"Avg Memory Delta: {avgMemDelta / metrics.Count:F1}MB");
        }
    }
}
```

### Step 2: Run Critical Operation Tests BEFORE Optimizations

```csharp
// Add to your main test suite
public void RunBaselineTests()
{
    var baseline = new PerformanceBaseline();
    int carId = 1;  // Your car ID
    
    // Test 1: DeleteDuplicateTrips current implementation
    baseline.MeasureOperationPerformance("DeleteDuplicateTrips(OLD)", () =>
    {
        DBHelper.DeleteDuplicateTrips();
    });
    
    // Test 2: KVS batch insert current (100 operations)
    baseline.MeasureOperationPerformance("KVS.InsertOrUpdate x100(OLD)", () =>
    {
        for (int i = 0; i < 100; i++)
        {
            KVS.InsertOrUpdate($"benchmark_key_{i}", i);
        }
    });
    
    // Test 3: AnalyzeChargingStates current implementation
    baseline.MeasureOperationPerformance("AnalyzeChargingStates(OLD)", () =>
    {
        Car car = Car.GetCarByID(carId);
        car.AnalyzeChargingStates();
    });
    
    // Test 4: Large SELECT * query
    baseline.MeasureOperationPerformance("SelectAllChargingData(OLD)", () =>
    {
        using (var dt = DBHelper.GetDataTable(@"
            SELECT * FROM charging WHERE CarID = @CarID", carId))
        {
            // Just fetching the data
        }
    });
    
    // Test 5: Journey aggregation query
    baseline.MeasureOperationPerformance("JourneyAggregation(OLD)", () =>
    {
        using (var dt = DBHelper.GetDataTable(@"
            SELECT COUNT(*) FROM journeys WHERE CarID = @CarID", carId))
        {
            // Just fetching
        }
    });
    
    baseline.GenerateBaselineReport("baseline_before_optimization.txt");
}
```

---

## <a name="testing"></a>2. TESTING STRATEGY

### Unit Test for Each Optimization

#### Test 1: KVS Batch Operations

```csharp
[TestClass]
public class KVSBatchTests
{
    [TestMethod]
    public void TestBatchInsertOrUpdate_CorrectCount()
    {
        // Arrange
        var items = new List<(string, object, string)>
        {
            ("kvs_test_1", 100, "ivalue"),
            ("kvs_test_2", 200, "ivalue"),
            ("kvs_test_3", 300, "ivalue")
        };
        
        // Act
        int result = KVS.BatchInsertOrUpdate(items);
        
        // Assert
        Assert.AreEqual(KVS.SUCCESS, result);
        
        // Verify all inserts succeeded
        foreach (var item in items)
        {
            KVS.Get(item.Item1, out int value);
            Assert.AreEqual((int)item.Item2, value);
        }
    }
    
    [TestMethod]
    public void TestBatchInsertOrUpdate_Performance()
    {
        // Generate many items
        var items = new List<(string, object, string)>();
        for (int i = 0; i < 1000; i++)
        {
            items.Add(($"kvs_perf_test_{i}", i * 10, "ivalue"));
        }
        
        var sw = Stopwatch.StartNew();
        int result = KVS.BatchInsertOrUpdate(items);
        sw.Stop();
        
        Assert.AreEqual(KVS.SUCCESS, result);
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, 
            $"Batch insert of 1000 items took {sw.ElapsedMilliseconds}ms (should be < 1000ms)");
        
        Console.WriteLine($"1000 items batch insert: {sw.ElapsedMilliseconds}ms");
    }
    
    [TestMethod]
    public void TestBatchInsertOrUpdate_UpdatesExisting()
    {
        // Pre-populate
        KVS.InsertOrUpdate("kvs_update_test", 111);
        
        // Batch update existing
        var items = new List<(string, object, string)>
        {
            ("kvs_update_test", 222, "ivalue")
        };
        
        int result = KVS.BatchInsertOrUpdate(items);
        
        // Verify update happened
        KVS.Get("kvs_update_test", out int value);
        Assert.AreEqual(222, value);
    }
}
```

#### Test 2: DeleteDuplicateTrips Optimization

```csharp
[TestClass]
public class DeleteDuplicateTripsTests
{
    [TestMethod]
    public void TestDeleteDuplicateTripsBatched_LessThanOneMinute()
    {
        var sw = Stopwatch.StartNew();
        DBHelper.DeleteDuplicateTripsBatched();  // NEW batched version
        sw.Stop();
        
        Assert.IsTrue(sw.ElapsedMilliseconds < 60000,
            $"DeleteDuplicateTrips took {sw.ElapsedMilliseconds}ms (should be < 60 seconds)");
        
        Console.WriteLine($"DeleteDuplicateTripsBatched: {sw.ElapsedMilliseconds}ms");
    }
    
    [TestMethod]
    public void TestDeleteDuplicateTripsBatched_NoTableLock()
    {
        // Start deletion in background
        var deleteTask = Task.Run(() => DBHelper.DeleteDuplicateTripsBatched());
        
        // Try to read from table while deletion is happening
        Task.Delay(100).Wait();  // Wait for deletion to start
        
        try
        {
            using (var dt = DBHelper.GetDataTable("SELECT COUNT(*) FROM drivestate LIMIT 1"))
            {
                Assert.IsNotNull(dt);
                // Should be able to read while deletion happens
            }
        }
        catch (MySqlException ex) when (ex.Message.Contains("Timeout"))
        {
            Assert.Fail("Table locked during DeleteDuplicateTripsBatched - lock contention issue!");
        }
        
        deleteTask.Wait(300000);  // Wait max 5 minutes
    }
}
```

#### Test 3: SELECT * Elimination

```csharp
[TestClass]
public class SelectOptimizationTests
{
    [TestMethod]
    public void TestSelectOptimized_SmallerMemoryFootprint()
    {
        var process = Process.GetCurrentProcess();
        
        // Test old SELECT *
        double memBefore1 = process.WorkingSet64 / (1024.0 * 1024.0);
        using (var dtStarQuery = DBHelper.GetDataTable("SELECT * FROM charging WHERE CarID = 1 LIMIT 10000"))
        {
            double memAfter1 = process.WorkingSet64 / (1024.0 * 1024.0);
            double deltaSelectStar = memAfter1 - memBefore1;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Task.Delay(100).Wait();
            
            // Test new optimized query
            double memBefore2 = process.WorkingSet64 / (1024.0 * 1024.0);
            using (var dtOptimized = DBHelper.GetDataTable(@"
                SELECT id, CarID, charge_energy_added, charger_power, Datum 
                FROM charging WHERE CarID = 1 LIMIT 10000"))
            {
                double memAfter2 = process.WorkingSet64 / (1024.0 * 1024.0);
                double deltaOptimized = memAfter2 - memBefore2;
                
                Console.WriteLine($"SELECT * memory delta: {deltaSelectStar:F1}MB");
                Console.WriteLine($"SELECT optimized memory delta: {deltaOptimized:F1}MB");
                
                Assert.IsTrue(deltaOptimized < deltaSelectStar,
                    "Optimized query should use less memory");
            }
        }
    }
}
```

---

## <a name="tools"></a>3. PERFORMANCE BENCHMARKING TOOLS

### MySQL Slow Query Log Analysis

```sql
-- Enable slow query log on RPi
SET GLOBAL slow_query_log = 'ON';
SET GLOBAL long_query_time = 1;  -- Log queries > 1 second
SET GLOBAL log_queries_not_using_indexes = 'ON';

-- Check slow query log
SELECT * FROM mysql.slow_log LIMIT 20\G

-- Find slowest queries
SELECT sql_text, COUNT(*) as exec_count, 
       AVG(query_time) as avg_time, 
       MAX(query_time) as max_time,
       SUM(rows_examined) as total_rows_examined
FROM mysql.slow_log
GROUP BY sql_text
ORDER BY max_time DESC
LIMIT 20;
```

### Performance Schema Analysis

```sql
-- Which queries are slowest historically
CREATE TABLE baseline_queries AS
SELECT object_schema, object_name, count_read, count_insert, count_update, count_delete
FROM performance_schema.table_io_waits_summary_by_table
WHERE object_schema NOT IN ('mysql', 'information_schema', 'performance_schema')
ORDER BY (count_read + count_insert + count_update + count_delete) DESC;

-- Check table access patterns
SELECT * FROM performance_schema.table_io_waits_summary_by_index_usage
WHERE object_schema NOT IN ('mysql', 'information_schema')
ORDER BY count_read DESC LIMIT 20\G

-- Find missing indexes (queries hitting full table scans)
SELECT OBJECT_SCHEMA, OBJECT_NAME, COUNT_STAR, COUNT_READ, COUNT_WRITE, COUNT_DELETE, COUNT_UPDATE
FROM performance_schema.table_io_waits_summary_by_table t
JOIN performance_schema.table_io_waits_summary_by_index_usage i
    ON t.OBJECT_SCHEMA = i.OBJECT_SCHEMA
    AND t.OBJECT_NAME = i.OBJECT_NAME
WHERE i.INDEX_NAME = 'PRIMARY'
AND t.COUNT_STAR > 0
ORDER BY COUNT_STAR DESC;
```

### Query Plan Analysis with EXPLAIN

```csharp
// Add to DBHelper for performance debugging
public static void AnalyzeQueryPlan(string sql)
{
    try
    {
        using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
        {
            con.Open();
            
            // Get query plan
            using (MySqlCommand cmd = new MySqlCommand($"EXPLAIN {sql}", con))
            {
                cmd.CommandTimeout = 60;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    Console.WriteLine("Query Plan Analysis:");
                    Console.WriteLine("id | select_type | table | type | possible_keys | key | key_len | ref | rows | filtered | Extra");
                    
                    while (dr.Read())
                    {
                        Console.WriteLine($"{dr["id"]} | {dr["select_type"]} | {dr["table"]} | " +
                                        $"{dr["type"]} | {dr["possible_keys"]} | {dr["key"]} | " +
                                        $"{dr["key_len"]} | {dr["ref"]} | {dr["rows"]} | " +  
                                        $"{dr["filtered"]} | {dr["Extra"]}");
                        
                        // RED FLAG: type = 'ALL' means full table scan
                        if (dr["type"].ToString() == "ALL")
                            Console.WriteLine("WARNING: Full table scan detected!");
                        
                        // RED FLAG: possible_keys is NULL but rows > 0
                        if (dr["possible_keys"] == DBNull.Value && dr["rows"].ToString() != "0")
                            Console.WriteLine("WARNING: No index available for this query!");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"EXPLAIN Analysis Error: {ex}");
    }
}

// Usage
AnalyzeQueryPlan("SELECT * FROM charging WHERE CarID = 1");
```

---

## <a name="comparison"></a>4. BEFORE/AFTER COMPARISON

### Create Comprehensive Comparison Report

```csharp
public class PerformanceComparison
{
    public struct ComparisonResult
    {
        public string Operation { get; set; }
        public long TimeBeforeMs { get; set; }
        public long TimeAfterMs { get; set; }
        public double ImprovementPercent { get; set; }
        public double MemoryBeforeMB { get; set; }
        public double MemoryAfterMB { get; set; }
        public bool IsSuccess { get; set; }
    }
    
    public static void GenerateComparisonReport(
        List<ComparisonResult> results, 
        string outputFile)
    {
        using (var writer = new System.IO.StreamWriter(outputFile))
        {
            writer.WriteLine("╔════════════════════════════════════════════════════════════════════════════════════╗");
            writer.WriteLine("║        TESLALOGGER SQL OPTIMIZATION - BEFORE/AFTER PERFORMANCE COMPARISON          ║");
            writer.WriteLine("╠════════════════════════════════════════════════════════════════════════════════════╣");
            writer.WriteLine($"║ Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"║ Host: {Environment.MachineName}");
            writer.WriteLine("╚════════════════════════════════════════════════════════════════════════════════════╝");
            writer.WriteLine();
            
            writer.WriteLine("┌─────────────────────────────────────────────────────────────────────────────────────┐");
            writer.WriteLine("│ EXECUTION TIME COMPARISON (milliseconds)                                           │");
            writer.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────┤");
            writer.WriteLine("│ Operation                          │ Before (ms) │ After (ms) │ Improvement % │");
            writer.WriteLine("├─────────────────────────────────────────────────────────────────────────────────────┤");
            
            foreach (var result in results.OrderByDescending(r => r.ImprovementPercent))
            {
                string status = result.IsSuccess ? "✓" : "✗";
                writer.WriteLine($"│ {result.Operation,-35} │ {result.TimeBeforeMs,11} │ {result.TimeAfterMs,10} │ {result.ImprovementPercent,13:F1}% │ {status}");
            }
            
            writer.WriteLine("└─────────────────────────────────────────────────────────────────────────────────────┘");
            writer.WriteLine();
            
            writer.WriteLine("SUMMARY STATISTICS:");
            writer.WriteLine($"  Average Improvement: {results.Average(r => r.ImprovementPercent):F1}%");
            writer.WriteLine($"  Best Improvement: {results.Max(r => r.ImprovementPercent):F1}% ({results.First(r => r.ImprovementPercent == results.Max(r2 => r2.ImprovementPercent)).Operation})");
            writer.WriteLine($"  Worst Improvement: {results.Min(r => r.ImprovementPercent):F1}% ({results.First(r => r.ImprovementPercent == results.Min(r2 => r2.ImprovementPercent)).Operation})");
            writer.WriteLine($"  Total Operations Tested: {results.Count}");
            writer.WriteLine($"  Successful Operations: {results.Count(r => r.IsSuccess)}/{results.Count}");
            writer.WriteLine();
            
            // Memory analysis
            double totalMemBefore = results.Sum(r => r.MemoryBeforeMB);
            double totalMemAfter = results.Sum(r => r.MemoryAfterMB);
            writer.WriteLine("MEMORY IMPACT:");
            writer.WriteLine($"  Total Memory Before: {totalMemBefore:F1}MB");
            writer.WriteLine($"  Total Memory After: {totalMemAfter:F1}MB");
            writer.WriteLine($"  Memory Reduction: {(totalMemBefore - totalMemAfter):F1}MB ({((totalMemBefore - totalMemAfter)/totalMemBefore * 100):F1}%)");
            writer.WriteLine();
            
            // Estimated overall performance gain
            double avgBefore = results.Average(r => r.TimeBeforeMs);
            double avgAfter = results.Average(r => r.TimeAfterMs);
            writer.WriteLine($"✓ OVERALL SYSTEM PERFORMANCE: {((avgBefore - avgAfter) / avgBefore * 100):F1}% faster on average");
            writer.WriteLine();
        }
    }
}
```

### Running the Comparison

```csharp
// Create test runner
public void ComparePerformance()
{
    var comparisons = new List<PerformanceComparison.ComparisonResult>();
    int carId = 1;
    
    // Test 1: KVS Batch Operations
    var kvsBefore = MeasureKVSPerformance(false);  // Current implementation
    var kvsAfter = MeasureKVSPerformance(true);    // New batch implementation
    comparisons.Add(new PerformanceComparison.ComparisonResult
    {
        Operation = "KVS.InsertOrUpdate x100",
        TimeBeforeMs = kvsBefore.Item1,
        TimeAfterMs = kvsAfter.Item1,
        ImprovementPercent = ((kvsBefore.Item1 - kvsAfter.Item1) / (double)kvsBefore.Item1) * 100,
        MemoryBeforeMB = kvsBefore.Item2,
        MemoryAfterMB = kvsAfter.Item2,
        IsSuccess = kvsAfter.Item1 < kvsBefore.Item1
    });
    
    // Test 2: DeleteDuplicateTrips
    var delBefore = MeasureDeleteDuplicateTripsPerformance(false);
    var delAfter = MeasureDeleteDuplicateTripsPerformance(true);
    comparisons.Add(new PerformanceComparison.ComparisonResult
    {
        Operation = "DeleteDuplicateTrips",
        TimeBeforeMs = delBefore.Item1,
        TimeAfterMs = delAfter.Item1,
        ImprovementPercent = ((delBefore.Item1 - delAfter.Item1) / (double)delBefore.Item1) * 100,
        MemoryBeforeMB = delBefore.Item2,
        MemoryAfterMB = delAfter.Item2,
        IsSuccess = delAfter.Item1 < delBefore.Item1
    });
    
    // Test 3+ more operations...
    
    PerformanceComparison.GenerateComparisonReport(comparisons, "performance_comparison.txt");
}
```

---

## <a name="monitoring"></a>5. CONTINUOUS MONITORING & ALERTING

### Real-time Performance Monitoring Dashboard

```csharp
public class PerformanceMonitor
{
    private List<PerformanceSnapshot> history = new List<PerformanceSnapshot>();
    
    public struct PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public int ActiveConnections { get; set; }
        public int SlowQueriesLastHour { get; set; }
        public double MemoryUsageMB { get; set; }
        public double CPUUsagePercent { get; set; }
        public int PendingOps { get; set; }
    }
    
    public PerformanceSnapshot CaptureSnapshot()
    {
        var process = Process.GetCurrentProcess();
        
        int activeConn = 0, slowQueries = 0;
        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
        {
            con.Open();
            
            // Get active connections
            using (MySqlCommand cmd = new MySqlCommand("SHOW PROCESSLIST", con))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        activeConn++;
                }
            }
            
            // Get recent slow queries
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM mysql.slow_log WHERE start_time > DATE_SUB(NOW(), INTERVAL 1 HOUR)",
                con))
            {
                slowQueries = (int)cmd.ExecuteScalar();
            }
        }
        
        // CPU calculation
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue();  // First call always returns 0
        System.Threading.Thread.Sleep(100);
        float cpuUsage = cpuCounter.NextValue();
        
        return new PerformanceSnapshot
        {
            Timestamp = DateTime.Now,
            ActiveConnections = activeConn,
            SlowQueriesLastHour = slowQueries,
            MemoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0),
            CPUUsagePercent = cpuUsage,
            PendingOps = 0  // TODO: track pending operations
        };
    }
    
    public void MonitorContinuously(int intervalSeconds = 60)
    {
        while (true)
        {
            var snapshot = CaptureSnapshot();
            history.Add(snapshot);
            
            // Check for alerts
            if (snapshot.SlowQueriesLastHour > 10)
                AlertSlow queries($"{snapshot.SlowQueriesLastHour} slow queries in last hour");
            
            if (snapshot.MemoryUsageMB > 700)  // 700MB on 1GB RPi
                AlertHighMemory($"Memory usage {snapshot.MemoryUsageMB:F0}MB (>700MB threshold)");
            
            if (snapshot.ActiveConnections > 8)
                AlertTooManyConnections($"{snapshot.ActiveConnections} connections (>8 threshold)");
            
            System.Threading.Thread.Sleep(intervalSeconds * 1000);
        }
    }
    
    private void AlertSlowQueries(string message) => Logfile.Log($"⚠ ALERT: {message}");
    private void AlertHighMemory(string message) => Logfile.Log($"⚠ ALERT: {message}");
    private void AlertTooManyConnections(string message) => Logfile.Log($"⚠ ALERT: {message}");
}
```

---

## <a name="troubleshooting"></a>6. TROUBLESHOOTING GUIDE

### Performance Not Improving?

```sql
-- Check if indexes are actually being used
ANALYZE TABLE charging;
ANALYZE TABLE chargingstate;
ANALYZE TABLE drivestate;

-- Verify indexes exist
SHOW INDEX FROM charging WHERE Column_name IN ('CarID', 'Datum');

-- Check index statistics
SELECT * FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'teslalogger'
AND COLUMN_NAME IN ('CarID', 'StartDate', 'Datum')
ORDER BY TABLE_NAME;
```

### Query Returning Wrong Results?

```sql
-- Verify data integrity after optimization
-- Before/after row counts should match
SELECT COUNT(*) as before_count FROM charging WHERE CarID = 1;

-- Check for duplicates
SELECT CarID, Datum, COUNT(*) as cnt
FROM charging
GROUP BY CarID, Datum
HAVING COUNT(*) > 1;

-- Validate calculations
SELECT id, charge_energy_added, charger_power, charger_voltage,
       (charger_power * 1000 / charger_voltage) as calculated_amperage,
       charger_actual_current as stored_amperage
FROM charging
WHERE charger_actua current != (charger_power * 1000 / charger_voltage)
LIMIT 10;
```

### Performance Degraded After Implementation?

```csharp
// Rollback procedure
public static void RollbackOptimizations()
{
    // 1. Restore MySQL config
    File.Copy("mysql.cnf.backup", "/etc/mysql/my.cnf", true);
    
    // 2. Revert code changes
    RunCommand("git checkout -- TeslaLogger/DBHelper.cs TeslaLogger/KVS.cs");
    
    // 3. Restart services
    RunCommand("sudo systemctl restart mysql");
    RunCommand("# Rebuild and restart application");
    
    Logfile.Log("Rollback completed - system restored to pre-optimization state");
}
```

### Connection Pool Issues?

```sql
-- Monitor connection pool
SHOW STATUS LIKE 'Threads%';
SHOW STATUS LIKE 'Connections';

-- Check max connections
SHOW VARIABLES LIKE 'max_connections';

-- See current connections
SHOW PROCESSLIST;

-- Kill idle connections
KILL CONNECTION_ID;
```

---

## IMPLEMENTATION CHECKLIST

- [ ] Run baseline performance tests BEFORE making changes
- [ ] Generate baseline_before_optimization.txt report
- [ ] Implement one optimization at a time
- [ ] Run benchmarks after each optimization
- [ ] Verify results with EXPLAIN queries
- [ ] Check MySQL slow query log for improvements
- [ ] Monitor memory usage on RPi
- [ ] Generate final comparison report
- [ ] Document findings and next steps
- [ ] Set up continuous monitoring dashboard

**Expected Results After All Optimizations**:
- 40-60% overall performance improvement
- DeleteDuplicateTrips: 60 seconds → 3-5 seconds (15x faster)
- KVS bulk updates: 10 seconds → 100ms (100x faster)
- Memory usage: 30-40% reduction
- CPU utilization: 20-30% reduction
- Zero functional regressions

