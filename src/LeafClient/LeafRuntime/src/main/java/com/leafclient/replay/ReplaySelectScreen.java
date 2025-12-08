package com.leafclient.replay;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.client.gui.widget.TextFieldWidget;
import net.minecraft.text.Text;

import java.io.File;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.List;

public class ReplaySelectScreen extends Screen {

    private final Screen parent;

    private ButtonWidget playButton;
    private ButtonWidget deleteButton;
    private ButtonWidget refreshButton;
    private TextFieldWidget searchField;

    private File[] allReplays = new File[0];

    // Simple list model (no EntryListWidget)
    private final List<File> visibleReplays = new ArrayList<>();
    private int selectedIndex = -1;

    // List layout
    private int listTop;
    private int listBottom;
    private int itemHeight = 22;
    private int listLeft;
    private int listRight;
    private int scrollOffset = 0; // number of items scrolled

    private static final SimpleDateFormat DATE_FORMAT = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");

    public ReplaySelectScreen(Screen parent) {
        super(Text.literal("Replay Browser"));
        this.parent = parent;
    }

    @Override
    protected void init() {
        MinecraftClient client = MinecraftClient.getInstance();
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod == null) {
            client.setScreen(parent);
            return;
        }

        allReplays = replayMod.getReplayFiles();
        Arrays.sort(allReplays, (a, b) -> Long.compare(b.lastModified(), a.lastModified()));

        // List bounds
        listTop = 40;
        listBottom = this.height - 70;
        listLeft = this.width / 2 - 180;
        listRight = this.width / 2 + 180;

        // Search box
        this.searchField = new TextFieldWidget(
                this.textRenderer,
                this.width / 2 - 150,
                15,
                300,
                18,
                Text.literal("Search")
        );
        this.searchField.setChangedListener(s -> applyFilter());
        this.addSelectableChild(this.searchField);

        // Buttons
        int center = this.width / 2;
        this.playButton = ButtonWidget.builder(Text.literal("Play"), b -> onPlay())
                .dimensions(center - 155, this.height - 60, 100, 20)
                .build();
        this.deleteButton = ButtonWidget.builder(Text.literal("Delete"), b -> onDelete())
                .dimensions(center - 50, this.height - 60, 100, 20)
                .build();
        this.refreshButton = ButtonWidget.builder(Text.literal("Refresh"), b -> {
                    reloadFromDisk();
                    applyFilter();
                })
                .dimensions(center + 55, this.height - 60, 100, 20)
                .build();

        this.addDrawableChild(this.playButton);
        this.addDrawableChild(this.deleteButton);
        this.addDrawableChild(this.refreshButton);

        // Initial list content
        reloadList();
        updateButtonStates();
    }

    @Override
    public void tick() {
        if (this.searchField != null) {
            this.searchField.tick();
        }
    }

    @Override
    public void close() {
        this.client.setScreen(parent);
    }

    @Override
    public boolean keyPressed(int keyCode, int scanCode, int modifiers) {
        if (this.searchField != null && this.searchField.keyPressed(keyCode, scanCode, modifiers)) {
            return true;
        }
        return super.keyPressed(keyCode, scanCode, modifiers);
    }

    @Override
    public boolean charTyped(char chr, int modifiers) {
        if (this.searchField != null && this.searchField.charTyped(chr, modifiers)) {
            return true;
        }
        return super.charTyped(chr, modifiers);
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (this.searchField != null && this.searchField.mouseClicked(mouseX, mouseY, button)) {
            return true;
        }

        // Handle list click
        if (mouseX >= listLeft && mouseX <= listRight && mouseY >= listTop && mouseY <= listBottom) {
            int indexInView = (int)((mouseY - listTop) / itemHeight);
            int absoluteIndex = scrollOffset + indexInView;
            if (absoluteIndex >= 0 && absoluteIndex < visibleReplays.size()) {
                selectedIndex = absoluteIndex;
                updateButtonStates();
            }
            return true;
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

   // NOTE: No @Override because in these mappings Screen/ParentElement use
    // mouseScrolled(double mouseX, double mouseY, double amount)
    public boolean mouseScrolled(double mouseX, double mouseY, double verticalAmount) {
        // Scroll list only when mouse is over the list area
        if (mouseX >= listLeft && mouseX <= listRight && mouseY >= listTop && mouseY <= listBottom) {
            int maxVisible = (listBottom - listTop) / itemHeight;
            int maxScroll = Math.max(0, visibleReplays.size() - maxVisible);

            // In these mappings, verticalAmount > 0 = scroll up, < 0 = scroll down
            scrollOffset -= (int) Math.signum(verticalAmount);
            if (scrollOffset < 0) scrollOffset = 0;
            if (scrollOffset > maxScroll) scrollOffset = maxScroll;
            return true;
        }

        // Call the 3‑arg super version
        return super.mouseScrolled(mouseX, mouseY, verticalAmount);
    }

    @Override
    public void render(DrawContext context, int mouseX, int mouseY, float delta) {
        this.renderBackground(context);

        // Title
        context.drawCenteredTextWithShadow(this.textRenderer, this.title, this.width / 2, 5, 0xFFFFFF);

        // List background
        fillListBackground(context);

        // Render list entries
        renderList(context, mouseX, mouseY);

        // Search field
        if (this.searchField != null) {
            this.searchField.render(context, mouseX, mouseY, delta);
        }

        super.render(context, mouseX, mouseY, delta);
    }

    private void fillListBackground(DrawContext context) {
        // simple dark rect
        int bgColor = 0x80000000;
        context.fill(listLeft, listTop, listRight, listBottom, bgColor);
    }

    private void renderList(DrawContext context, int mouseX, int mouseY) {
        MinecraftClient client = MinecraftClient.getInstance();

        int maxVisible = (listBottom - listTop) / itemHeight;
        int start = scrollOffset;
        int end = Math.min(visibleReplays.size(), start + maxVisible);

        for (int i = start; i < end; i++) {
            int idxInView = i - start;
            int y = listTop + idxInView * itemHeight;

            boolean hovered = mouseX >= listLeft && mouseX <= listRight && mouseY >= y && mouseY < y + itemHeight;
            boolean selected = (i == selectedIndex);

            int bg = selected ? 0x60FFFFFF : (hovered ? 0x40FFFFFF : 0x20000000);
            context.fill(listLeft + 2, y, listRight - 2, y + itemHeight - 1, bg);

            File file = visibleReplays.get(i);
            String name = file.getName();
            if (name.endsWith(".lfreplay")) {
                name = name.substring(0, name.length() - ".lfreplay".length());
            }

            long modified = file.lastModified();
            String dateStr = DATE_FORMAT.format(new Date(modified));
            long sizeKb = Math.max(1, file.length() / 1024);
            String details = dateStr + " | " + sizeKb + " KB";

            int textX = listLeft + 6;
            int nameY = y + 3;
            int detailsY = y + 12;

            context.drawTextWithShadow(client.textRenderer, name, textX, nameY, 0xFFFFFF);
            context.drawTextWithShadow(client.textRenderer, details, textX, detailsY, 0x777777);
        }
    }

    private void reloadFromDisk() {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod == null) return;
        allReplays = replayMod.getReplayFiles();
        Arrays.sort(allReplays, (a, b) -> Long.compare(b.lastModified(), a.lastModified()));
    }

    private void reloadList() {
        visibleReplays.clear();
        if (allReplays != null) {
            visibleReplays.addAll(Arrays.asList(allReplays));
        }
        selectedIndex = -1;
        scrollOffset = 0;
    }

    private void applyFilter() {
        String query = this.searchField != null ? this.searchField.getText().trim().toLowerCase() : "";

        visibleReplays.clear();
        if (allReplays != null) {
            for (File file : allReplays) {
                if (query.isEmpty() || file.getName().toLowerCase().contains(query)) {
                    visibleReplays.add(file);
                }
            }
        }
        selectedIndex = visibleReplays.isEmpty() ? -1 : 0;
        scrollOffset = 0;
        updateButtonStates();
    }

    private File getSelectedFile() {
        if (selectedIndex < 0 || selectedIndex >= visibleReplays.size()) {
            return null;
        }
        return visibleReplays.get(selectedIndex);
    }

    private void onPlay() {
        System.out.println("[ReplayUI] PLAY BUTTON CLICKED");
    
        MinecraftClient client = MinecraftClient.getInstance();
    
        File replayFile = getSelectedFile();
        if (replayFile == null) {
            System.out.println("[ReplayUI] No selected entry!");
            return;
        }
    
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod == null) {
            System.out.println("[ReplayUI] ReplayMod == null");
            return;
        }
    
        System.out.println("[ReplayUI] Selected file: " + replayFile.getAbsolutePath());
    
        if (!replayFile.exists()) {
            System.out.println("[ReplayUI] File does NOT exist on disk");
            return;
        }
    
        // Instead of starting playback now, queue it to start once a world is loaded
        replayMod.queueReplay(replayFile);
    
        // Go back to parent (likely TitleScreen)
        client.setScreen(parent);
    
        System.out.println("[ReplayUI] Replay queued. Load or join a world to start playback.");
    }

    private void onDelete() {
        File f = getSelectedFile();
        if (f == null) return;

        if (f.exists() && f.delete()) {
            reloadFromDisk();
            applyFilter();  
        }
    }

    private void updateButtonStates() {
        boolean hasSelection = getSelectedFile() != null;
        if (this.playButton != null) this.playButton.active = hasSelection;
        if (this.deleteButton != null) this.deleteButton.active = hasSelection;
    }
}
