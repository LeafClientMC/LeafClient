package com.leaf.bootstrap;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.List;
public class Main {

    private static final String RUNTIME_JAR = "LeafRuntime-1.0.0.jar";
    private static final int MIN_JAVA_VERSION = 17;

    public static void main(String[] args) {
        System.out.println("------------------------------------------");
        System.out.println("|     LeafClient Bootstrap v1.0.0        |");
        System.out.println("------------------------------------------");

        try {
            validateJavaVersion();
            validateRuntimeJar();

            File gameDir = extractGameDirFromArgs(args);
            if (gameDir == null) {
                throw new RuntimeException("Could not determine --gameDir from arguments");
            }
            installRuntimeMod(gameDir);
            List<String> launchArgs = buildForwardLaunchArguments(args);
            launchRuntime(launchArgs);

        } catch (Exception e) {
            System.err.println("X Bootstrap failed: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }

    private static void validateJavaVersion() {
        String version = System.getProperty("java.version");
        System.out.println("[Bootstrap] Java version: " + version);

        try {
            String[] parts = version.split("\\.");
            int major = Integer.parseInt(parts[0]);

            if (major < MIN_JAVA_VERSION) {
                throw new RuntimeException(
                        "Java " + MIN_JAVA_VERSION + "+ required. Current: " + version
                );
            }

            System.out.println("  - Java version OK");

        } catch (Exception e) {
            throw new RuntimeException("Failed to parse Java version: " + version);
        }
    }

    private static void validateRuntimeJar() {
        File jar = new File(RUNTIME_JAR);
        if (!jar.exists()) {
            throw new RuntimeException("Runtime JAR not found: " + RUNTIME_JAR);
        }
        System.out.println("  - Runtime JAR found: " + jar.getAbsolutePath());
    }

    private static File extractGameDirFromArgs(String[] args) {
        for (int i = 0; i < args.length - 1; i++) {
            if ("--gameDir".equals(args[i])) {
                return new File(args[i + 1]);
            }
        }
        return null;
    }

    private static void installRuntimeMod(File gameDir) throws IOException {
        File modsDir = new File(gameDir, "mods");
        if (!modsDir.exists() && !modsDir.mkdirs()) {
            throw new IOException("Failed to create mods directory: " + modsDir.getAbsolutePath());
        }

        File sourceJar = new File(RUNTIME_JAR);
        File targetJar = new File(modsDir, "leafclient-runtime-1.0.0.jar");

        System.out.println("[Bootstrap] Installing runtime mod to: " + targetJar.getAbsolutePath());
        Files.copy(sourceJar.toPath(), targetJar.toPath(), StandardCopyOption.REPLACE_EXISTING);
        System.out.println("  - Runtime mod installed");
    }

    private static List<String> buildForwardLaunchArguments(String[] launcherArgs) {
        List<String> args = new ArrayList<>();
        String javaHome = System.getProperty("java.home");
        String javaExe = javaHome + File.separator + "bin" + File.separator + "java";
        if (System.getProperty("os.name").toLowerCase().contains("win")) {
            javaExe += ".exe";
        }
        args.add(javaExe);
        for (String arg : launcherArgs) {
            args.add(arg);
        }

        return args;
    }

    private static void launchRuntime(List<String> args) throws IOException {
        System.out.println("\n[Bootstrap] Launching runtime (forwarding original Fabric command)...");
        System.out.println("  Command: " + String.join(" ", args));

        ProcessBuilder pb = new ProcessBuilder(args);
        pb.inheritIO();
        pb.directory(new File(System.getProperty("user.dir")));

        Process process = pb.start();
        System.out.println("  - Runtime process started (PID: " + process.pid() + ")");

        try {
            int exitCode = process.waitFor();
            System.out.println("\n[Bootstrap] Runtime exited with code: " + exitCode);
            System.exit(exitCode);
        } catch (InterruptedException e) {
            System.err.println("[Bootstrap] Process interrupted");
            process.destroy();
            System.exit(1);
        }
    }
}
