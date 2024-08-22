// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef PIXELS_H
#define PIXELS_H

enum ChannelType {
    EChannelType_NONE, EChannelType_int8, EChannelType_float32, EChannelType_float64
};

int ChannelType_bytes(ChannelType value);

class PixInfo {
public:

    std::string pixformat;
    ChannelType channel_type = EChannelType_NONE;
    int num_channels = 0;
    int channel_bytes = 0;
    unsigned char *channel_list = nullptr;
    SingleLink_Node<int> channels[256];

    size_t pix_stride = 0;

protected:
    PixInfo( );
    ~PixInfo();
public:

    static P_INSTANCE(PixInfo)  get(std::string pixformat);
    void Release();
};

class PixConversion;
typedef void (*ChannelConvert)(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel);

class PixConversion {
public:
    int *channel_map;
    
    P_INSTANCE(PixInfo) from;
    P_INSTANCE(PixInfo) to;

    ChannelConvert channel_convert;
protected:
    PixConversion();
public:
    static P_INSTANCE(PixConversion)  get(P_INSTANCE(PixInfo) from, P_INSTANCE(PixInfo) to);
    void Release();

    void convert(P_ELEMENTS(void) src, P_ELEMENTS(void) dst);
};

#endif //PIXELS_H
