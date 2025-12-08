package com.leafclient.screen;

import com.leafclient.LeafClient;
import com.leafclient.macro.ChatMacro;
import com.leafclient.modules.ChatMacrosMod;
import com.leafclient.ui.CustomButtonWidget;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.TextFieldWidget;
import net.minecraft.text.Text;
import org.lwjgl.glfw.GLFW;

import java.util.ArrayList;
import java.util.List;

public class ChatMacrosScreen extends Screen {
    
    private final Screen parent;
    private ChatMacrosMod mod;
    private List<MacroEntry> entries = new ArrayList<>();
    private int scrollOffset = 0;
    
    private CustomButtonWidget addButton;
    private CustomButtonWidget doneButton;
    
    public ChatMacrosScreen(Screen parent) {
        super(Text.literal("Chat Macros"));
        this.parent = parent;
    }
    
    @Override
    protected void init() {
        // Clear all existing widgets
        clearChildren();
        
        mod = LeafClient.modManager.getMod(ChatMacrosMod.class);
        if (mod == null) {
            close();
            return;
        }
        
        entries.clear();
        
        // Create entry widgets for each macro
        int yPos = 40;
        for (int i = 0; i < mod.getMacros().size(); i++) {
            ChatMacro macro = mod.getMacros().get(i);
            entries.add(new MacroEntry(macro, i, yPos));
            yPos += 60;
        }
        
        // Add button
        addButton = CustomButtonWidget.customBuilder(Text.literal("Add Macro"), button -> {
            ChatMacro newMacro = new ChatMacro("New Macro", "Hello!", GLFW.GLFW_KEY_H, true, false, 0);
            mod.addMacro(newMacro);
            init(); // Refresh
        }).dimensions(this.width / 2 - 155, this.height - 28, 150, 20).build();
        
        addDrawableChild(addButton);
        
        // Done button
        doneButton = CustomButtonWidget.customBuilder(Text.literal("Done"), button -> {
            close();
        }).dimensions(this.width / 2 + 5, this.height - 28, 150, 20).build();
        
        addDrawableChild(doneButton);
        
        // Initialize all entry widgets
        for (MacroEntry entry : entries) {
            entry.init();
        }
    }
    
    @Override
    public void render(DrawContext context, int mouseX, int mouseY, float delta) {
        renderBackground(context);
        
        context.drawCenteredTextWithShadow(this.textRenderer, this.title, this.width / 2, 10, 0xFFFFFF);
        
        // Render macro entries
        for (MacroEntry entry : entries) {
            entry.render(context, mouseX, mouseY, delta);
        }
        
        super.render(context, mouseX, mouseY, delta);
    }
    
    @Override
    public void close() {
        if (client != null) {
            client.setScreen(parent);
        }
    }
    
    private class MacroEntry {
        private ChatMacro macro;
        private int index;
        private int y;
        
        private TextFieldWidget nameField;
        private TextFieldWidget messageField;
        private CustomButtonWidget keyButton;
        private CustomButtonWidget toggleButton;
        private CustomButtonWidget deleteButton;
        private TextFieldWidget delayField;
        
        private boolean awaitingKey = false;
        
        public MacroEntry(ChatMacro macro, int index, int y) {
            this.macro = macro;
            this.index = index;
            this.y = y;
        }
        
        public void init() {
            // Name field
            nameField = new TextFieldWidget(textRenderer, 10, y, 100, 18, Text.literal("Name"));
            nameField.setMaxLength(32);
            nameField.setText(macro.getName());
            nameField.setChangedListener(text -> {
                macro.setName(text);
                mod.saveMacros();
            });
            addSelectableChild(nameField);
            addDrawableChild(nameField);
            
            // Message field
            messageField = new TextFieldWidget(textRenderer, 115, y, 200, 18, Text.literal("Message"));
            messageField.setMaxLength(256);
            messageField.setText(macro.getMessage());
            messageField.setChangedListener(text -> {
                macro.setMessage(text);
                mod.saveMacros();
            });
            addSelectableChild(messageField);
            addDrawableChild(messageField);
            
            // Key button
            keyButton = CustomButtonWidget.customBuilder(Text.literal(getKeyName(macro.getKey())), button -> {
                awaitingKey = true;
                button.setMessage(Text.literal("Press a key..."));
            }).dimensions(320, y, 80, 18).build();
            addDrawableChild(keyButton);
            
            // Toggle button
            toggleButton = CustomButtonWidget.customBuilder(
                Text.literal(macro.isEnabled() ? "ON" : "OFF"), 
                button -> {
                    macro.setEnabled(!macro.isEnabled());
                    button.setMessage(Text.literal(macro.isEnabled() ? "ON" : "OFF"));
                    mod.saveMacros();
                }
            ).dimensions(405, y, 40, 18).build();
            addDrawableChild(toggleButton);
            
            // Delay field
            delayField = new TextFieldWidget(textRenderer, 10, y + 22, 60, 18, Text.literal("Delay"));
            delayField.setMaxLength(6);
            delayField.setText(String.valueOf(macro.getDelayMs()));
            delayField.setChangedListener(text -> {
                try {
                    int delay = Integer.parseInt(text);
                    macro.setDelayMs(Math.max(0, delay));
                    mod.saveMacros();
                } catch (NumberFormatException e) {
                    // Ignore invalid input
                }
            });
            addSelectableChild(delayField);
            addDrawableChild(delayField);
            
            // Repeat toggle
            CustomButtonWidget repeatButton = CustomButtonWidget.customBuilder(
                Text.literal("Repeat: " + (macro.isAllowRepeat() ? "ON" : "OFF")),
                button -> {
                    macro.setAllowRepeat(!macro.isAllowRepeat());
                    button.setMessage(Text.literal("Repeat: " + (macro.isAllowRepeat() ? "ON" : "OFF")));
                    mod.saveMacros();
                }
            ).dimensions(75, y + 22, 100, 18).build();
            addDrawableChild(repeatButton);
            
            // Delete button
            deleteButton = CustomButtonWidget.customBuilder(Text.literal("Delete"), button -> {
                mod.removeMacro(macro);
                ChatMacrosScreen.this.init(); // Refresh the screen
            }).dimensions(405, y + 22, 40, 18).build();
            addDrawableChild(deleteButton);
        }
        
        public void render(DrawContext context, int mouseX, int mouseY, float delta) {
            // Draw background box
            context.fill(5, y - 2, width - 5, y + 42, 0x80000000);
            
            // Draw labels
            context.drawTextWithShadow(textRenderer, "Delay (ms):", 10, y + 44, 0xAAAAAA);
        }
        
        private String getKeyName(int key) {
            if (key == GLFW.GLFW_KEY_UNKNOWN) return "None";
            String name = GLFW.glfwGetKeyName(key, 0);
            if (name != null) return name.toUpperCase();
            
            // Fallback for special keys
            switch (key) {
                case GLFW.GLFW_KEY_SPACE: return "SPACE";
                case GLFW.GLFW_KEY_ENTER: return "ENTER";
                case GLFW.GLFW_KEY_TAB: return "TAB";
                case GLFW.GLFW_KEY_BACKSPACE: return "BACKSPACE";
                case GLFW.GLFW_KEY_LEFT_SHIFT: return "LSHIFT";
                case GLFW.GLFW_KEY_RIGHT_SHIFT: return "RSHIFT";
                case GLFW.GLFW_KEY_LEFT_CONTROL: return "LCTRL";
                case GLFW.GLFW_KEY_RIGHT_CONTROL: return "RCTRL";
                case GLFW.GLFW_KEY_LEFT_ALT: return "LALT";
                case GLFW.GLFW_KEY_RIGHT_ALT: return "RALT";
                default: return "KEY_" + key;
            }
        }
    }
    
    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        // Check if any entry is awaiting key input
        for (MacroEntry entry : entries) {
            if (entry.awaitingKey) {
                entry.macro.setKey(keyCode);
                entry.keyButton.setMessage(Text.literal(entry.getKeyName(keyCode)));
                entry.awaitingKey = false;
                mod.saveMacros();
                return true;
            }
        }
        
        return super.keyPressed(keyCode, scanCode, modifiers);
    }
    
    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double amount) {
        scrollOffset -= (int) (amount * 20);
        scrollOffset = Math.max(0, Math.min(scrollOffset, Math.max(0, entries.size() * 60 - height + 100)));
        return true;
    }
}