package com.leafclient;

import java.io.File;
import java.io.IOException;

public class LauncherSignalManager {

    private static final String APPDATA = System.getenv("APPDATA");
    private static final File LEAFCLIENT_DIR = new File(APPDATA, "LeafClient");

    public static void sendSignal(String signal) {
        if (!LEAFCLIENT_DIR.exists()) {
            LEAFCLIENT_DIR.mkdirs();
        }

        File signalFile = new File(LEAFCLIENT_DIR, signal);
        try {
            signalFile.createNewFile();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}