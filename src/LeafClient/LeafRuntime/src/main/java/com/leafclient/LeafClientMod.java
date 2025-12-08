package com.leafclient;

public interface LeafClientMod {
    String MOD_ID = "leafclient";

    String getName();
    boolean isEnabled();
    void setEnabled(boolean enabled);
}