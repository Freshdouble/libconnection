#include <sys/types.h>
#ifndef __USE_MISC
#define __USE_MISC
#endif

#include <sys/socket.h>
#include <sys/ioctl.h>
#include <net/if.h>

#include <linux/can.h>
#include <linux/can/raw.h>
#include <string.h>

#include <stdlib.h>
#include <stdint.h>
#include <unistd.h>

typedef struct
{
    int socket;
}sockcan_t;

sockcan_t* GetInstance(const char* caninterfacename)
{
    if(caninterfacename == NULL)
        return NULL;

    if(strlen(caninterfacename) > 20 || strlen(caninterfacename) == 0)
        return NULL;

    sockcan_t* instance = malloc(sizeof(sockcan_t));
    if(instance != NULL)
    {
        instance->socket = socket(PF_CAN, SOCK_RAW, CAN_RAW);
        struct ifreq ifr;
        strcpy(ifr.ifr_name, caninterfacename);
        if(ioctl(instance->socket, SIOCGIFINDEX, &ifr) < 0)
        {
            free(instance);
            return NULL;
        }
        struct sockaddr_can addr;
        addr.can_family = AF_CAN;
        addr.can_ifindex = ifr.ifr_ifindex;
        if(bind(instance->socket, (struct sockaddr *)&addr, sizeof(addr)) < 0)
        {
            free(instance);
            return NULL;
        }
    }
    return instance;
}

void DeleteInstance(sockcan_t* ptr)
{
    free(ptr);
}

int SendBytes(sockcan_t* instance, int canID, const uint8_t* data, int length)
{
    if(length > 8)
        return 0;
    
    struct can_frame frame;
    canID &= 0x7FF;
    frame.can_id = canID;
    memcpy(frame.data, data, length);
    frame.can_dlc = length;
    return write(instance->socket, &frame, sizeof(frame));
}

int ReceiveBytes(sockcan_t* instance, uint8_t* data, int length, uint16_t* canID)
{
    struct can_frame frame;
    int bytes_read = read(instance->socket, &frame, sizeof(frame));
    if(bytes_read > 0)
    {
        int toRead = length < frame.can_dlc ? length : frame.can_dlc;
        memcpy(data, frame.data, toRead);
        if(canID != NULL)
        {
            *canID = frame.can_id;
        }
        return toRead;
    }
    else
    {
        return 0;
    }
}