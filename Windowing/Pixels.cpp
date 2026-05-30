// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <cstring>
#include "CrystalCatalystLibrary//CrystalCatalystLibrary.h"

#include <cassert>
#include <iostream>
#include <algorithm>
#include <cctype>

namespace NewAge {
    int ChannelType_bytes(ChannelType value) {
        if (value == EChannelType_NONE) return 0;
        if (value == EChannelType_int8) return 1;
        if (value == EChannelType_float32) return 4;
        if (value == EChannelType_float64) return 8;

        /*TODO*/ //ErrorSystem

        return -1;
    }

    PixInfo::PixInfo( ) {
        //memset(channels, 0, sizeof(channels));
    }

    PixInfo::~PixInfo() {
        if (channel_list != nullptr) delete []channel_list;
        for (int i = 0; i < 256; i++) {
            SingleLink_Node<int>* cur = channels[i].next;
            while (cur) {
                SingleLink_Node<int>* next = cur->next;
                cur->next = nullptr;
                delete cur;
                cur = next;
            }
            channels[i].next = nullptr;
        }
    }

    P_INSTANCE(PixInfo)  PixInfo::get(utf8_string_struct pixformat) {
        P_INSTANCE(PixInfo) P = new PixInfo();

        std::string pix = (std::string) pixformat.c_str;

        size_t colon = pix.find(':');

        if (colon == std::string::npos) {
            /* TODO */ //ErrorSystem IO
            P->Release();
            return nullptr;
        }

        std::string before_colon = pix.substr(0, colon);
        std::string after_colon = pix.substr(colon + 1);

        std::transform(before_colon.begin(), before_colon.end(), before_colon.begin(),
                       [](unsigned char c) { return std::tolower(c); });

        P->pixformat_storage = before_colon + ":" + after_colon;
        P->pixformat = P->pixformat_storage.c_str();

        std::string _channel_type = pix.substr(colon + 1);
        if (_channel_type == "int8") P->channel_type = EChannelType_int8;
        else if (_channel_type == "float32") P->channel_type = EChannelType_float32;
        else if (_channel_type == "float64") P->channel_type = EChannelType_float64;

        if (P->channel_type == EChannelType_NONE) {
            /* TODO */ //ErrorSystem IO
            P->Release();
            return nullptr;
        }

        P->num_channels = colon;
        P->channel_bytes = ChannelType_bytes(P->channel_type);

        P->channel_list = new unsigned char[P->num_channels+1];

        for (int32_t i = 0; i < colon; i++) {
            unsigned char id = pixformat[i];

            if (id >= 'a' && id <= 'z') id = 'A' + id - 'a';

            P->channel_list[i] = id;
            P->channels[id].tail_add(i);
        }

        P->channel_list[P->num_channels] = 0;

        P->pix_stride = colon * P->channel_bytes;

        return P;
    }

    void PixInfo::Release() {
        delete this;
    }

    PixConversion::PixConversion() {
        channel_map = nullptr;
        from = to = nullptr;
        channel_convert = nullptr;
    }

    void ChannelConvert_int8_int8(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((int8_t *)dst)[dest_channel] =((int8_t *)src)[src_channel];
    }

    void ChannelConvert_int8_float32(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((int8_t *)dst)[dest_channel] =(int8_t)(((float32 *)src)[src_channel] * 0xFF);
    }

    void ChannelConvert_int8_float64(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((int8_t *)dst)[dest_channel] =(int8_t)(((float64 *)src)[src_channel] * 0xFF);
    }

    void ChannelConvert_float32_int8(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((float *)dst)[dest_channel] =(float)((uint8_t *)src)[src_channel] / 255.0f;
    }

    void ChannelConvert_float32_float32(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((float *)dst)[dest_channel] =((float *)src)[src_channel];
    }

    void ChannelConvert_float32_float64(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((float *)dst)[dest_channel] =(float)(((double *)src)[src_channel]);
    }

    void ChannelConvert_float64_int8(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((double *)dst)[dest_channel] =(double)((uint8_t *)src)[src_channel] / 255.0f;
    }

    void ChannelConvert_float64_float32(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((double *)dst)[dest_channel] =((float *)src)[src_channel];
    }

    void ChannelConvert_float64_float64(P_INSTANCE(PixConversion) This, P_ELEMENTS(void) src, int src_channel, P_ELEMENTS(void) dst, int dest_channel) {
        ((double *)dst)[dest_channel] = ((double *)src)[src_channel];
    }

    P_INSTANCE(PixConversion)  PixConversion::get(P_INSTANCE(PixInfo) from, P_INSTANCE(PixInfo) to) {
        P_INSTANCE(PixConversion) conversion  = new PixConversion();

        conversion->from = from;
        conversion->to = to;

        conversion->channel_map = new int[to->num_channels];

        for (int i=0; i<to->num_channels; i++) {
            unsigned char dest_channel = to->channel_list[i];

            //SingleLink_Node<int> *n = src_type->channels[dest_channel].next;

            if (from->channels[dest_channel].next == nullptr) {
                conversion->channel_map[i] = -1;
                continue;
            }

            int src_index = from->channels[dest_channel].next->value;
            assert(src_index >=0 && src_index < from->num_channels);

            conversion->channel_map[i] = src_index;
        }

        ChannelConvert cv = nullptr;

        switch (to->channel_type) {
            case EChannelType_int8: {
                switch (from->channel_type) {
                    case EChannelType_int8: {
                        cv = ChannelConvert_int8_int8;
                    }
                    break;
                    case EChannelType_float32: {
                        cv = ChannelConvert_int8_float32;
                    }
                    break;
                    case EChannelType_float64: {
                        cv = ChannelConvert_int8_float64;
                    }
                    break;
                    default: {

                    }
                    break;
                }
            }
            break;
            case EChannelType_float32: {
                switch (from->channel_type) {
                    case EChannelType_int8: {
                        cv = ChannelConvert_float32_int8;
                    }
                    break;
                    case EChannelType_float32: {
                        cv = ChannelConvert_float32_float32;
                    }
                    break;
                    case EChannelType_float64: {
                        cv = ChannelConvert_float32_float64;
                    }
                    break;
                    default: {

                    }
                    break;
                }
            }
            break;
            case EChannelType_float64: {
                switch (from->channel_type) {
                    case EChannelType_int8: {
                        cv = ChannelConvert_float64_int8;
                    }
                    break;
                    case EChannelType_float32: {
                        cv = ChannelConvert_float64_float32;

                    }
                    break;
                    case EChannelType_float64: {
                        cv = ChannelConvert_float64_float64;
                    }
                    break;
                    default: {

                    }
                    break;
                }
            }
            break;
            default: {

            }
            break;
        }

        assert(cv != nullptr);
        conversion->channel_convert = cv;

        return conversion;
    }

    void PixConversion::Release() {
        delete[] channel_map;
        delete this;
    }

    void PixConversion::convert(P_ELEMENTS(void) src, P_ELEMENTS(void) dst) {
        for (int i=0; i<to->num_channels; i++) {
            int src_channel = channel_map[i];
            if (src_channel < 0) {
                // Set default values for missing channels
                if (to->channel_list[i] == 'A') {
                    if (to->channel_type == EChannelType_int8) {
                        ((uint8_t*)dst)[i] = 255;
                    } else if (to->channel_type == EChannelType_float32) {
                        ((float*)dst)[i] = 1.0f;
                    } else if (to->channel_type == EChannelType_float64) {
                        ((double*)dst)[i] = 1.0;
                    }
                } else {
                    if (to->channel_type == EChannelType_int8) {
                        ((uint8_t*)dst)[i] = 0;
                    } else if (to->channel_type == EChannelType_float32) {
                        ((float*)dst)[i] = 0.0f;
                    } else if (to->channel_type == EChannelType_float64) {
                        ((double*)dst)[i] = 0.0;
                    }
                }
                continue;
            }

            channel_convert(this, src, src_channel, dst, i);
        }
    };

    bool pix_free(uint8_t *data) {
        delete[] data;
        return true;
    }

    PixData Pixels_ConvertPixels(utf8_string_struct pixformat, utf8_string_struct pixformat_dest, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) {
        PixData Ret = {};

        if (!pixdata || !pixformat || !pixformat_dest) return Ret;

        P_INSTANCE(PixInfo) src_pixinfo = PixInfo::get(pixformat);
        if (!src_pixinfo) return Ret;

        P_INSTANCE(PixInfo) dst_pixinfo = PixInfo::get(pixformat_dest);
        if (!dst_pixinfo) {
            src_pixinfo->Release();
            return Ret;
        }

        if ((std::string)src_pixinfo->pixformat.c_str == (std::string)dst_pixinfo->pixformat.c_str) {
            
            src_pixinfo->Release();
            dst_pixinfo->Release();
            return Ret;
        }

        P_INSTANCE(PixConversion) conversion = PixConversion::get(src_pixinfo, dst_pixinfo);
        if (!conversion) {
            src_pixinfo->Release();
            dst_pixinfo->Release();
            return Ret;
        }

        size_t pix_count = width * height;
        size_t total_dst_size = pix_count * dst_pixinfo->pix_stride;

        Ret.height = height;
        Ret.width = width;
        Ret.pix_data = new uint8_t[total_dst_size];
        Ret.pix_data_length = total_dst_size;
        Ret.pix_format = pixformat_dest;
        Ret.pix_data_free = pix_free;

        int8_t *src_ptr = (int8_t *)pixdata;
        int8_t *dst_ptr = (int8_t *)Ret.pix_data;

        for (int i=0; i < pix_count; i++) {
            conversion->convert(src_ptr, dst_ptr);
            src_ptr += src_pixinfo->pix_stride;
            dst_ptr += dst_pixinfo->pix_stride;
        }

        conversion->Release();
        src_pixinfo->Release();
        dst_pixinfo->Release();

        return Ret;
    }
}