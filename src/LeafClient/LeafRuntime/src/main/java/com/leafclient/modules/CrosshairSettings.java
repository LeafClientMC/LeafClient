package com.leafclient.modules;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.awt.Color;

public class CrosshairSettings {
    private static final String FILE_PATH = "crosshair_settings.json";
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();

    public Color color = Color.WHITE;
    public int size = 4;
    public int gap = 2;
    public boolean dot = true;

    public static CrosshairSettings load() {
        File file = new File(FILE_PATH);
        if (file.exists()) {
            try (FileReader reader = new FileReader(file)) {
                return GSON.fromJson(reader, CrosshairSettings.class);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        return new CrosshairSettings();
    }

    public void save() {
        try (FileWriter writer = new FileWriter(FILE_PATH)) {
            GSON.toJson(this, writer);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}