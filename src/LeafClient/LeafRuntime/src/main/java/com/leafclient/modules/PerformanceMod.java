package com.leafclient.modules;

import com.sun.management.OperatingSystemMXBean;
import java.lang.management.ManagementFactory;

public class PerformanceMod implements ILeafMod {
    private boolean enabled = false;
    private boolean showMemory = true;
    private boolean showCpu = true;
    private final OperatingSystemMXBean osBean;
    
    // CPU caching
    private double cachedCpuUsage = 0.0;
    private long lastCpuUpdate = 0;
    private static final long CPU_UPDATE_INTERVAL = 1000; // Update every 1 second
    
    // Memory caching
    private int cachedMemoryPercent = 0;
    private long lastMemoryUpdate = 0;
    private static final long MEMORY_UPDATE_INTERVAL = 500; // Update every 0.5 seconds
    
    // CPU calculation variables
    private long lastCpuTime = 0;
    private long lastSystemTime = 0;
    private boolean initialized = false;

    public PerformanceMod() {
        osBean = (OperatingSystemMXBean) ManagementFactory.getOperatingSystemMXBean();
        updateMemoryUsage();
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] Performance " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Performance";
    }

    public boolean isShowMemory() {
        return showMemory;
    }

    public void setShowMemory(boolean showMemory) {
        this.showMemory = showMemory;
    }

    public boolean isShowCpu() {
        return showCpu;
    }

    public void setShowCpu(boolean showCpu) {
        this.showCpu = showCpu;
    }

    public double getCpuUsage() {
        long currentTime = System.currentTimeMillis();
        
        // Only update CPU usage periodically
        if (currentTime - lastCpuUpdate >= CPU_UPDATE_INTERVAL) {
            updateCpuUsage();
        }
        
        return cachedCpuUsage;
    }
    
    private void updateCpuUsage() {
        try {
            long currentCpuTime = osBean.getProcessCpuTime();
            long currentSystemTime = System.nanoTime();
            
            if (!initialized) {
                lastCpuTime = currentCpuTime;
                lastSystemTime = currentSystemTime;
                initialized = true;
                cachedCpuUsage = 0.0;
            } else {
                // Calculate the difference
                long cpuTimeDiff = currentCpuTime - lastCpuTime;
                long systemTimeDiff = currentSystemTime - lastSystemTime;
                
                if (systemTimeDiff > 0 && cpuTimeDiff >= 0) {
                    // Get number of available processors
                    int availableProcessors = Runtime.getRuntime().availableProcessors();
                    
                    double cpuUsage = (double) cpuTimeDiff / systemTimeDiff;
                    cachedCpuUsage = Math.min(100.0, (cpuUsage / availableProcessors) * 100.0);
                }
                
                lastCpuTime = currentCpuTime;
                lastSystemTime = currentSystemTime;
            }
            
        } catch (Exception e) {
            cachedCpuUsage = 0.0;
            e.printStackTrace();
        }
        
        lastCpuUpdate = System.currentTimeMillis();
    }
    
    public int getMemoryPercent() {
        long currentTime = System.currentTimeMillis();
        
        // Only update memory usage periodically
        if (currentTime - lastMemoryUpdate >= MEMORY_UPDATE_INTERVAL) {
            updateMemoryUsage();
        }
        
        return cachedMemoryPercent;
    }
    
    private void updateMemoryUsage() {
        try {
            // Get the committed virtual memory size (actual process memory usage)
            long processMemory = osBean.getCommittedVirtualMemorySize();
            
            // Get total physical memory
            long totalPhysicalMemory = osBean.getTotalPhysicalMemorySize();
            
            if (totalPhysicalMemory > 0 && processMemory > 0) {
                // Calculate percentage of system memory used by this process
                cachedMemoryPercent = (int) ((processMemory * 100L) / totalPhysicalMemory);
            } else {
                // Fallback to JVM heap usage if system memory info unavailable
                long maxMemory = Runtime.getRuntime().maxMemory();
                long totalMemory = Runtime.getRuntime().totalMemory();
                long freeMemory = Runtime.getRuntime().freeMemory();
                long usedMemory = totalMemory - freeMemory;
                
                cachedMemoryPercent = (int) (usedMemory * 100L / maxMemory);
            }
        } catch (Exception e) {
            // Fallback to JVM heap usage
            long maxMemory = Runtime.getRuntime().maxMemory();
            long totalMemory = Runtime.getRuntime().totalMemory();
            long freeMemory = Runtime.getRuntime().freeMemory();
            long usedMemory = totalMemory - freeMemory;
            
            cachedMemoryPercent = (int) (usedMemory * 100L / maxMemory);
        }
        
        lastMemoryUpdate = System.currentTimeMillis();
    }
}
