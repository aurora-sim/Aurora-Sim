// **********************************************************************
//
// Copyright (c) 2003-2009 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// ICE_LICENSE file included in this distribution.
//
// **********************************************************************

// Ice version 3.3.1
// Generated from file `Murmur.ice'

/// Original source at https://github.com/vgaessler/whisper_server

#if __MonoCS__

using _System = System;
using _Microsoft = Microsoft;
#else

using _System = global::System;
using _Microsoft = global::Microsoft;
#endif

namespace Murmur
{
    public class User : _System.ICloneable
    {
        #region Slice data members

        public int session;

        public int userid;

        public bool mute;

        public bool deaf;

        public bool suppress;

        public bool selfMute;

        public bool selfDeaf;

        public int channel;

        public string name;

        public int onlinesecs;

        public int bytespersec;

        public int version;

        public string release;

        public string os;

        public string osversion;

        public string identity;

        public string context;

        public string comment;

        public byte[] address;

        public bool tcponly;

        public int idlesecs;

        #endregion

        #region Constructors

        public User()
        {
        }

        public User(int session, int userid, bool mute, bool deaf, bool suppress, bool selfMute, bool selfDeaf, int channel, string name, int onlinesecs, int bytespersec, int version, string release, string os, string osversion, string identity, string context, string comment, byte[] address, bool tcponly, int idlesecs)
        {
            this.session = session;
            this.userid = userid;
            this.mute = mute;
            this.deaf = deaf;
            this.suppress = suppress;
            this.selfMute = selfMute;
            this.selfDeaf = selfDeaf;
            this.channel = channel;
            this.name = name;
            this.onlinesecs = onlinesecs;
            this.bytespersec = bytespersec;
            this.version = version;
            this.release = release;
            this.os = os;
            this.osversion = osversion;
            this.identity = identity;
            this.context = context;
            this.comment = comment;
            this.address = address;
            this.tcponly = tcponly;
            this.idlesecs = idlesecs;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            h__ = 5 * h__ + session.GetHashCode();
            h__ = 5 * h__ + userid.GetHashCode();
            h__ = 5 * h__ + mute.GetHashCode();
            h__ = 5 * h__ + deaf.GetHashCode();
            h__ = 5 * h__ + suppress.GetHashCode();
            h__ = 5 * h__ + selfMute.GetHashCode();
            h__ = 5 * h__ + selfDeaf.GetHashCode();
            h__ = 5 * h__ + channel.GetHashCode();
            if(name != null)
            {
                h__ = 5 * h__ + name.GetHashCode();
            }
            h__ = 5 * h__ + onlinesecs.GetHashCode();
            h__ = 5 * h__ + bytespersec.GetHashCode();
            h__ = 5 * h__ + version.GetHashCode();
            if(release != null)
            {
                h__ = 5 * h__ + release.GetHashCode();
            }
            if(os != null)
            {
                h__ = 5 * h__ + os.GetHashCode();
            }
            if(osversion != null)
            {
                h__ = 5 * h__ + osversion.GetHashCode();
            }
            if(identity != null)
            {
                h__ = 5 * h__ + identity.GetHashCode();
            }
            if(context != null)
            {
                h__ = 5 * h__ + context.GetHashCode();
            }
            if(comment != null)
            {
                h__ = 5 * h__ + comment.GetHashCode();
            }
            if(address != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(address);
            }
            h__ = 5 * h__ + tcponly.GetHashCode();
            h__ = 5 * h__ + idlesecs.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            User o__ = (User)other__;
            if(!session.Equals(o__.session))
            {
                return false;
            }
            if(!userid.Equals(o__.userid))
            {
                return false;
            }
            if(!mute.Equals(o__.mute))
            {
                return false;
            }
            if(!deaf.Equals(o__.deaf))
            {
                return false;
            }
            if(!suppress.Equals(o__.suppress))
            {
                return false;
            }
            if(!selfMute.Equals(o__.selfMute))
            {
                return false;
            }
            if(!selfDeaf.Equals(o__.selfDeaf))
            {
                return false;
            }
            if(!channel.Equals(o__.channel))
            {
                return false;
            }
            if(name == null)
            {
                if(o__.name != null)
                {
                    return false;
                }
            }
            else
            {
                if(!name.Equals(o__.name))
                {
                    return false;
                }
            }
            if(!onlinesecs.Equals(o__.onlinesecs))
            {
                return false;
            }
            if(!bytespersec.Equals(o__.bytespersec))
            {
                return false;
            }
            if(!version.Equals(o__.version))
            {
                return false;
            }
            if(release == null)
            {
                if(o__.release != null)
                {
                    return false;
                }
            }
            else
            {
                if(!release.Equals(o__.release))
                {
                    return false;
                }
            }
            if(os == null)
            {
                if(o__.os != null)
                {
                    return false;
                }
            }
            else
            {
                if(!os.Equals(o__.os))
                {
                    return false;
                }
            }
            if(osversion == null)
            {
                if(o__.osversion != null)
                {
                    return false;
                }
            }
            else
            {
                if(!osversion.Equals(o__.osversion))
                {
                    return false;
                }
            }
            if(identity == null)
            {
                if(o__.identity != null)
                {
                    return false;
                }
            }
            else
            {
                if(!identity.Equals(o__.identity))
                {
                    return false;
                }
            }
            if(context == null)
            {
                if(o__.context != null)
                {
                    return false;
                }
            }
            else
            {
                if(!context.Equals(o__.context))
                {
                    return false;
                }
            }
            if(comment == null)
            {
                if(o__.comment != null)
                {
                    return false;
                }
            }
            else
            {
                if(!comment.Equals(o__.comment))
                {
                    return false;
                }
            }
            if(address == null)
            {
                if(o__.address != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(address, o__.address))
                {
                    return false;
                }
            }
            if(!tcponly.Equals(o__.tcponly))
            {
                return false;
            }
            if(!idlesecs.Equals(o__.idlesecs))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(User lhs__, User rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(User lhs__, User rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeInt(session);
            os__.writeInt(userid);
            os__.writeBool(mute);
            os__.writeBool(deaf);
            os__.writeBool(suppress);
            os__.writeBool(selfMute);
            os__.writeBool(selfDeaf);
            os__.writeInt(channel);
            os__.writeString(name);
            os__.writeInt(onlinesecs);
            os__.writeInt(bytespersec);
            os__.writeInt(version);
            os__.writeString(release);
            os__.writeString(os);
            os__.writeString(osversion);
            os__.writeString(identity);
            os__.writeString(context);
            os__.writeString(comment);
            os__.writeByteSeq(address);
            os__.writeBool(tcponly);
            os__.writeInt(idlesecs);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            session = is__.readInt();
            userid = is__.readInt();
            mute = is__.readBool();
            deaf = is__.readBool();
            suppress = is__.readBool();
            selfMute = is__.readBool();
            selfDeaf = is__.readBool();
            channel = is__.readInt();
            name = is__.readString();
            onlinesecs = is__.readInt();
            bytespersec = is__.readInt();
            version = is__.readInt();
            release = is__.readString();
            os = is__.readString();
            osversion = is__.readString();
            identity = is__.readString();
            context = is__.readString();
            comment = is__.readString();
            address = is__.readByteSeq();
            tcponly = is__.readBool();
            idlesecs = is__.readInt();
        }

        #endregion
    }

    public class Channel : _System.ICloneable
    {
        #region Slice data members

        public int id;

        public string name;

        public int parent;

        public int[] links;

        public string description;

        public bool temporary;

        public int position;

        #endregion

        #region Constructors

        public Channel()
        {
        }

        public Channel(int id, string name, int parent, int[] links, string description, bool temporary, int position)
        {
            this.id = id;
            this.name = name;
            this.parent = parent;
            this.links = links;
            this.description = description;
            this.temporary = temporary;
            this.position = position;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            h__ = 5 * h__ + id.GetHashCode();
            if(name != null)
            {
                h__ = 5 * h__ + name.GetHashCode();
            }
            h__ = 5 * h__ + parent.GetHashCode();
            if(links != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(links);
            }
            if(description != null)
            {
                h__ = 5 * h__ + description.GetHashCode();
            }
            h__ = 5 * h__ + temporary.GetHashCode();
            h__ = 5 * h__ + position.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            Channel o__ = (Channel)other__;
            if(!id.Equals(o__.id))
            {
                return false;
            }
            if(name == null)
            {
                if(o__.name != null)
                {
                    return false;
                }
            }
            else
            {
                if(!name.Equals(o__.name))
                {
                    return false;
                }
            }
            if(!parent.Equals(o__.parent))
            {
                return false;
            }
            if(links == null)
            {
                if(o__.links != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(links, o__.links))
                {
                    return false;
                }
            }
            if(description == null)
            {
                if(o__.description != null)
                {
                    return false;
                }
            }
            else
            {
                if(!description.Equals(o__.description))
                {
                    return false;
                }
            }
            if(!temporary.Equals(o__.temporary))
            {
                return false;
            }
            if(!position.Equals(o__.position))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(Channel lhs__, Channel rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(Channel lhs__, Channel rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeInt(id);
            os__.writeString(name);
            os__.writeInt(parent);
            os__.writeIntSeq(links);
            os__.writeString(description);
            os__.writeBool(temporary);
            os__.writeInt(position);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            id = is__.readInt();
            name = is__.readString();
            parent = is__.readInt();
            links = is__.readIntSeq();
            description = is__.readString();
            temporary = is__.readBool();
            position = is__.readInt();
        }

        #endregion
    }

    public class Group : _System.ICloneable
    {
        #region Slice data members

        public string name;

        public bool inherited;

        public bool inherit;

        public bool inheritable;

        public int[] add;

        public int[] remove;

        public int[] members;

        #endregion

        #region Constructors

        public Group()
        {
        }

        public Group(string name, bool inherited, bool inherit, bool inheritable, int[] add, int[] remove, int[] members)
        {
            this.name = name;
            this.inherited = inherited;
            this.inherit = inherit;
            this.inheritable = inheritable;
            this.add = add;
            this.remove = remove;
            this.members = members;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            if(name != null)
            {
                h__ = 5 * h__ + name.GetHashCode();
            }
            h__ = 5 * h__ + inherited.GetHashCode();
            h__ = 5 * h__ + inherit.GetHashCode();
            h__ = 5 * h__ + inheritable.GetHashCode();
            if(add != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(add);
            }
            if(remove != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(remove);
            }
            if(members != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(members);
            }
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            Group o__ = (Group)other__;
            if(name == null)
            {
                if(o__.name != null)
                {
                    return false;
                }
            }
            else
            {
                if(!name.Equals(o__.name))
                {
                    return false;
                }
            }
            if(!inherited.Equals(o__.inherited))
            {
                return false;
            }
            if(!inherit.Equals(o__.inherit))
            {
                return false;
            }
            if(!inheritable.Equals(o__.inheritable))
            {
                return false;
            }
            if(add == null)
            {
                if(o__.add != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(add, o__.add))
                {
                    return false;
                }
            }
            if(remove == null)
            {
                if(o__.remove != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(remove, o__.remove))
                {
                    return false;
                }
            }
            if(members == null)
            {
                if(o__.members != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(members, o__.members))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(Group lhs__, Group rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(Group lhs__, Group rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeString(name);
            os__.writeBool(inherited);
            os__.writeBool(inherit);
            os__.writeBool(inheritable);
            os__.writeIntSeq(add);
            os__.writeIntSeq(remove);
            os__.writeIntSeq(members);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            name = is__.readString();
            inherited = is__.readBool();
            inherit = is__.readBool();
            inheritable = is__.readBool();
            add = is__.readIntSeq();
            remove = is__.readIntSeq();
            members = is__.readIntSeq();
        }

        #endregion
    }

    public abstract class PermissionWrite
    {
        public const int value = 1;
    }

    public abstract class PermissionTraverse
    {
        public const int value = 2;
    }

    public abstract class PermissionEnter
    {
        public const int value = 4;
    }

    public abstract class PermissionSpeak
    {
        public const int value = 8;
    }

    public abstract class PermissionWhisper
    {
        public const int value = 256;
    }

    public abstract class PermissionMuteDeafen
    {
        public const int value = 16;
    }

    public abstract class PermissionMove
    {
        public const int value = 32;
    }

    public abstract class PermissionMakeChannel
    {
        public const int value = 64;
    }

    public abstract class PermissionMakeTempChannel
    {
        public const int value = 1024;
    }

    public abstract class PermissionLinkChannel
    {
        public const int value = 128;
    }

    public abstract class PermissionTextMessage
    {
        public const int value = 512;
    }

    public abstract class PermissionKick
    {
        public const int value = 65536;
    }

    public abstract class PermissionBan
    {
        public const int value = 131072;
    }

    public abstract class PermissionRegister
    {
        public const int value = 262144;
    }

    public abstract class PermissionRegisterSelf
    {
        public const int value = 524288;
    }

    public class ACL : _System.ICloneable
    {
        #region Slice data members

        public bool applyHere;

        public bool applySubs;

        public bool inherited;

        public int userid;

        public string group;

        public int allow;

        public int deny;

        #endregion

        #region Constructors

        public ACL()
        {
        }

        public ACL(bool applyHere, bool applySubs, bool inherited, int userid, string group, int allow, int deny)
        {
            this.applyHere = applyHere;
            this.applySubs = applySubs;
            this.inherited = inherited;
            this.userid = userid;
            this.group = group;
            this.allow = allow;
            this.deny = deny;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            h__ = 5 * h__ + applyHere.GetHashCode();
            h__ = 5 * h__ + applySubs.GetHashCode();
            h__ = 5 * h__ + inherited.GetHashCode();
            h__ = 5 * h__ + userid.GetHashCode();
            if(group != null)
            {
                h__ = 5 * h__ + group.GetHashCode();
            }
            h__ = 5 * h__ + allow.GetHashCode();
            h__ = 5 * h__ + deny.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            ACL o__ = (ACL)other__;
            if(!applyHere.Equals(o__.applyHere))
            {
                return false;
            }
            if(!applySubs.Equals(o__.applySubs))
            {
                return false;
            }
            if(!inherited.Equals(o__.inherited))
            {
                return false;
            }
            if(!userid.Equals(o__.userid))
            {
                return false;
            }
            if(group == null)
            {
                if(o__.group != null)
                {
                    return false;
                }
            }
            else
            {
                if(!group.Equals(o__.group))
                {
                    return false;
                }
            }
            if(!allow.Equals(o__.allow))
            {
                return false;
            }
            if(!deny.Equals(o__.deny))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(ACL lhs__, ACL rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(ACL lhs__, ACL rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeBool(applyHere);
            os__.writeBool(applySubs);
            os__.writeBool(inherited);
            os__.writeInt(userid);
            os__.writeString(group);
            os__.writeInt(allow);
            os__.writeInt(deny);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            applyHere = is__.readBool();
            applySubs = is__.readBool();
            inherited = is__.readBool();
            userid = is__.readInt();
            group = is__.readString();
            allow = is__.readInt();
            deny = is__.readInt();
        }

        #endregion
    }

    public class Ban : _System.ICloneable
    {
        #region Slice data members

        public byte[] address;

        public int bits;

        public string name;

        public string hash;

        public string reason;

        public long start;

        public int duration;

        #endregion

        #region Constructors

        public Ban()
        {
        }

        public Ban(byte[] address, int bits, string name, string hash, string reason, long start, int duration)
        {
            this.address = address;
            this.bits = bits;
            this.name = name;
            this.hash = hash;
            this.reason = reason;
            this.start = start;
            this.duration = duration;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            if(address != null)
            {
                h__ = 5 * h__ + IceUtilInternal.Arrays.GetHashCode(address);
            }
            h__ = 5 * h__ + bits.GetHashCode();
            if(name != null)
            {
                h__ = 5 * h__ + name.GetHashCode();
            }
            if(hash != null)
            {
                h__ = 5 * h__ + hash.GetHashCode();
            }
            if(reason != null)
            {
                h__ = 5 * h__ + reason.GetHashCode();
            }
            h__ = 5 * h__ + start.GetHashCode();
            h__ = 5 * h__ + duration.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            Ban o__ = (Ban)other__;
            if(address == null)
            {
                if(o__.address != null)
                {
                    return false;
                }
            }
            else
            {
                if(!IceUtilInternal.Arrays.Equals(address, o__.address))
                {
                    return false;
                }
            }
            if(!bits.Equals(o__.bits))
            {
                return false;
            }
            if(name == null)
            {
                if(o__.name != null)
                {
                    return false;
                }
            }
            else
            {
                if(!name.Equals(o__.name))
                {
                    return false;
                }
            }
            if(hash == null)
            {
                if(o__.hash != null)
                {
                    return false;
                }
            }
            else
            {
                if(!hash.Equals(o__.hash))
                {
                    return false;
                }
            }
            if(reason == null)
            {
                if(o__.reason != null)
                {
                    return false;
                }
            }
            else
            {
                if(!reason.Equals(o__.reason))
                {
                    return false;
                }
            }
            if(!start.Equals(o__.start))
            {
                return false;
            }
            if(!duration.Equals(o__.duration))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(Ban lhs__, Ban rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(Ban lhs__, Ban rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeByteSeq(address);
            os__.writeInt(bits);
            os__.writeString(name);
            os__.writeString(hash);
            os__.writeString(reason);
            os__.writeLong(start);
            os__.writeInt(duration);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            address = is__.readByteSeq();
            bits = is__.readInt();
            name = is__.readString();
            hash = is__.readString();
            reason = is__.readString();
            start = is__.readLong();
            duration = is__.readInt();
        }

        #endregion
    }

    public class LogEntry : _System.ICloneable
    {
        #region Slice data members

        public int timestamp;

        public string txt;

        #endregion

        #region Constructors

        public LogEntry()
        {
        }

        public LogEntry(int timestamp, string txt)
        {
            this.timestamp = timestamp;
            this.txt = txt;
        }

        #endregion

        #region ICloneable members

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            h__ = 5 * h__ + timestamp.GetHashCode();
            if(txt != null)
            {
                h__ = 5 * h__ + txt.GetHashCode();
            }
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(other__ == null)
            {
                return false;
            }
            if(GetType() != other__.GetType())
            {
                return false;
            }
            LogEntry o__ = (LogEntry)other__;
            if(!timestamp.Equals(o__.timestamp))
            {
                return false;
            }
            if(txt == null)
            {
                if(o__.txt != null)
                {
                    return false;
                }
            }
            else
            {
                if(!txt.Equals(o__.txt))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(LogEntry lhs__, LogEntry rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(LogEntry lhs__, LogEntry rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshalling support

        public void write__(IceInternal.BasicStream os__)
        {
            os__.writeInt(timestamp);
            os__.writeString(txt);
        }

        public void read__(IceInternal.BasicStream is__)
        {
            timestamp = is__.readInt();
            txt = is__.readString();
        }

        #endregion
    }

    public enum ChannelInfo
    {
        ChannelDescription,
        ChannelPosition
    }

    public enum UserInfo
    {
        UserName,
        UserEmail,
        UserComment,
        UserHash,
        UserPassword
    }

    public class Tree : Ice.ObjectImpl
    {
        #region Slice data members

        public Murmur.Channel c;

        public Murmur.Tree[] children;

        public Murmur.User[] users;

        #endregion

        #region Constructors

        public Tree()
        {
        }

        public Tree(Murmur.Channel c, Murmur.Tree[] children, Murmur.User[] users)
        {
            this.c = c;
            this.children = children;
            this.users = users;
        }

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::Tree"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            if(c == null)
            {
                Murmur.Channel tmp__ = new Murmur.Channel();
                tmp__.write__(os__);
            }
            else
            {
                c.write__(os__);
            }
            if(children == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(children.Length);
                for(int ix__ = 0; ix__ < children.Length; ++ix__)
                {
                    os__.writeObject(children[ix__]);
                }
            }
            if(users == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(users.Length);
                for(int ix__ = 0; ix__ < users.Length; ++ix__)
                {
                    (users[ix__] == null ? new Murmur.User() : users[ix__]).write__(os__);
                }
            }
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            if(c == null)
            {
                c = new Murmur.Channel();
            }
            c.read__(is__);
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 4);
                children = new Murmur.Tree[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    IceInternal.ArrayPatcher<Murmur.Tree> spx = new IceInternal.ArrayPatcher<Murmur.Tree>("::Murmur::Tree", children, ix__);
                    is__.readObject(spx);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 42);
                users = new Murmur.User[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    users[ix__] = new Murmur.User();
                    users[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Tree was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Tree was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class MurmurException : Ice.UserException
    {
        #region Constructors

        public MurmurException()
        {
        }

        public MurmurException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::MurmurException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = 0;
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is MurmurException))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(MurmurException lhs__, MurmurException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(MurmurException lhs__, MurmurException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::MurmurException");
            os__.startWriteSlice();
            os__.endWriteSlice();
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::MurmurException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::MurmurException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidSessionException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidSessionException()
        {
        }

        public InvalidSessionException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidSessionException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidSessionException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidSessionException lhs__, InvalidSessionException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidSessionException lhs__, InvalidSessionException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidSessionException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidSessionException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidSessionException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidChannelException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidChannelException()
        {
        }

        public InvalidChannelException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidChannelException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidChannelException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidChannelException lhs__, InvalidChannelException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidChannelException lhs__, InvalidChannelException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidChannelException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidChannelException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidChannelException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidServerException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidServerException()
        {
        }

        public InvalidServerException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidServerException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidServerException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidServerException lhs__, InvalidServerException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidServerException lhs__, InvalidServerException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidServerException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidServerException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidServerException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class ServerBootedException : Murmur.MurmurException
    {
        #region Constructors

        public ServerBootedException()
        {
        }

        public ServerBootedException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::ServerBootedException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is ServerBootedException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(ServerBootedException lhs__, ServerBootedException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(ServerBootedException lhs__, ServerBootedException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::ServerBootedException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::ServerBootedException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::ServerBootedException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class ServerFailureException : Murmur.MurmurException
    {
        #region Constructors

        public ServerFailureException()
        {
        }

        public ServerFailureException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::ServerFailureException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is ServerFailureException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(ServerFailureException lhs__, ServerFailureException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(ServerFailureException lhs__, ServerFailureException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::ServerFailureException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::ServerFailureException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::ServerFailureException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidUserException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidUserException()
        {
        }

        public InvalidUserException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidUserException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidUserException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidUserException lhs__, InvalidUserException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidUserException lhs__, InvalidUserException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidUserException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidUserException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidUserException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidTextureException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidTextureException()
        {
        }

        public InvalidTextureException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidTextureException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidTextureException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidTextureException lhs__, InvalidTextureException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidTextureException lhs__, InvalidTextureException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidTextureException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidTextureException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidTextureException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public class InvalidCallbackException : Murmur.MurmurException
    {
        #region Constructors

        public InvalidCallbackException()
        {
        }

        public InvalidCallbackException(_System.Exception ex__) : base(ex__)
        {
        }

        #endregion

        public override string ice_name()
        {
            return "Murmur::InvalidCallbackException";
        }

        #region Object members

        public override int GetHashCode()
        {
            int h__ = base.GetHashCode();
            return h__;
        }

        public override bool Equals(object other__)
        {
            if(other__ == null)
            {
                return false;
            }
            if(object.ReferenceEquals(this, other__))
            {
                return true;
            }
            if(!(other__ is InvalidCallbackException))
            {
                return false;
            }
            if(!base.Equals(other__))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Comparison members

        public static bool operator==(InvalidCallbackException lhs__, InvalidCallbackException rhs__)
        {
            return Equals(lhs__, rhs__);
        }

        public static bool operator!=(InvalidCallbackException lhs__, InvalidCallbackException rhs__)
        {
            return !Equals(lhs__, rhs__);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeString("::Murmur::InvalidCallbackException");
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readString();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidCallbackException was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "exception Murmur::InvalidCallbackException was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public interface ServerCallback : Ice.Object, ServerCallbackOperations_, ServerCallbackOperationsNC_
    {
    }

    public abstract class ContextServer
    {
        public const int value = 1;
    }

    public abstract class ContextChannel
    {
        public const int value = 2;
    }

    public abstract class ContextUser
    {
        public const int value = 4;
    }

    public interface ServerContextCallback : Ice.Object, ServerContextCallbackOperations_, ServerContextCallbackOperationsNC_
    {
    }

    public interface ServerAuthenticator : Ice.Object, ServerAuthenticatorOperations_, ServerAuthenticatorOperationsNC_
    {
    }

    public interface ServerUpdatingAuthenticator : Ice.Object, ServerUpdatingAuthenticatorOperations_, ServerUpdatingAuthenticatorOperationsNC_, Murmur.ServerAuthenticator
    {
    }

    public interface Server : Ice.Object, ServerOperations_, ServerOperationsNC_
    {
    }

    public interface MetaCallback : Ice.Object, MetaCallbackOperations_, MetaCallbackOperationsNC_
    {
    }

    public interface Meta : Ice.Object, MetaOperations_, MetaOperationsNC_
    {
    }
}

namespace Murmur
{
    public interface TreePrx : Ice.ObjectPrx
    {
    }

    public interface ServerCallbackPrx : Ice.ObjectPrx
    {
        void userConnected(Murmur.User state);
        void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void userDisconnected(Murmur.User state);
        void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void userStateChanged(Murmur.User state);
        void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelCreated(Murmur.Channel state);
        void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelRemoved(Murmur.Channel state);
        void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelStateChanged(Murmur.Channel state);
        void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerContextCallbackPrx : Ice.ObjectPrx
    {
        void contextAction(string action, Murmur.User usr, int session, int channelid);
        void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerAuthenticatorPrx : Ice.ObjectPrx
    {
        int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups);
        int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__);

        bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);
        bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int nameToId(string name);
        int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__);

        string idToName(int id);
        string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        byte[] idToTexture(int id);
        byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerUpdatingAuthenticatorPrx : Murmur.ServerAuthenticatorPrx
    {
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int unregisterUser(int id);
        int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter);
        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__);

        int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);
        int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int setTexture(int id, byte[] tex);
        int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerPrx : Ice.ObjectPrx
    {
        bool isRunning();
        bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__);

        void start();
        void start(_System.Collections.Generic.Dictionary<string, string> context__);

        void stop();
        void stop(_System.Collections.Generic.Dictionary<string, string> context__);

        void delete();
        void delete(_System.Collections.Generic.Dictionary<string, string> context__);

        int id();
        int id(_System.Collections.Generic.Dictionary<string, string> context__);

        void addCallback(Murmur.ServerCallbackPrx cb);
        void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeCallback(Murmur.ServerCallbackPrx cb);
        void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void setAuthenticator(Murmur.ServerAuthenticatorPrx auth);
        void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__);

        string getConf(string key);
        string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, string> getAllConf();
        _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__);

        void setConf(string key, string value);
        void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__);

        void setSuperuserPassword(string pw);
        void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.LogEntry[] getLog(int first, int last);
        Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers();
        _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels();
        _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Tree getTree();
        Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Ban[] getBans();
        Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__);

        void setBans(Murmur.Ban[] bans);
        void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__);

        void kickUser(int session, string reason);
        void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.User getState(int session);
        Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__);

        void setState(Murmur.User state);
        void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void sendMessage(int session, string text);
        void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__);

        bool hasPermission(int session, int channelid, int perm);
        bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__);

        void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx);
        void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeContextCallback(Murmur.ServerContextCallbackPrx cb);
        void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Channel getChannelState(int channelid);
        Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__);

        void setChannelState(Murmur.Channel state);
        void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeChannel(int channelid);
        void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__);

        int addChannel(string name, int parent);
        int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__);

        void sendMessageChannel(int channelid, bool tree, string text);
        void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__);

        void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit);
        void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__);

        void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit);
        void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__);

        void addUserToGroup(int channelid, int session, string group);
        void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeUserFromGroup(int channelid, int session, string group);
        void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__);

        void redirectWhisperGroup(int session, string source, string target);
        void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids);
        _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names);
        _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__);

        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        void unregisterUser(int userid);
        void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);
        void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid);
        _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter);
        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__);

        int verifyPassword(string name, string pw);
        int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__);

        byte[] getTexture(int userid);
        byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        void setTexture(int userid, byte[] tex);
        void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface MetaCallbackPrx : Ice.ObjectPrx
    {
        void started(Murmur.ServerPrx srv);
        void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__);

        void stopped(Murmur.ServerPrx srv);
        void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface MetaPrx : Ice.ObjectPrx
    {
        Murmur.ServerPrx getServer(int id);
        Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx newServer();
        Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx[] getBootedServers();
        Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx[] getAllServers();
        Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, string> getDefaultConf();
        _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__);

        void getVersion(out int major, out int minor, out int patch, out string text);
        void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__);

        void addCallback(Murmur.MetaCallbackPrx cb);
        void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeCallback(Murmur.MetaCallbackPrx cb);
        void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);
    }
}

namespace Murmur
{
    public interface ServerCallbackOperations_
    {
        void userConnected(Murmur.User state, Ice.Current current__);

        void userDisconnected(Murmur.User state, Ice.Current current__);

        void userStateChanged(Murmur.User state, Ice.Current current__);

        void channelCreated(Murmur.Channel state, Ice.Current current__);

        void channelRemoved(Murmur.Channel state, Ice.Current current__);

        void channelStateChanged(Murmur.Channel state, Ice.Current current__);
    }

    public interface ServerCallbackOperationsNC_
    {
        void userConnected(Murmur.User state);

        void userDisconnected(Murmur.User state);

        void userStateChanged(Murmur.User state);

        void channelCreated(Murmur.Channel state);

        void channelRemoved(Murmur.Channel state);

        void channelStateChanged(Murmur.Channel state);
    }

    public interface ServerContextCallbackOperations_
    {
        void contextAction(string action, Murmur.User usr, int session, int channelid, Ice.Current current__);
    }

    public interface ServerContextCallbackOperationsNC_
    {
        void contextAction(string action, Murmur.User usr, int session, int channelid);
    }

    public interface ServerAuthenticatorOperations_
    {
        int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, Ice.Current current__);

        bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        int nameToId(string name, Ice.Current current__);

        string idToName(int id, Ice.Current current__);

        byte[] idToTexture(int id, Ice.Current current__);
    }

    public interface ServerAuthenticatorOperationsNC_
    {
        int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups);

        bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);

        int nameToId(string name);

        string idToName(int id);

        byte[] idToTexture(int id);
    }

    public interface ServerUpdatingAuthenticatorOperations_ : Murmur.ServerAuthenticatorOperations_
    {
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        int unregisterUser(int id, Ice.Current current__);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, Ice.Current current__);

        int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        int setTexture(int id, byte[] tex, Ice.Current current__);
    }

    public interface ServerUpdatingAuthenticatorOperationsNC_ : Murmur.ServerAuthenticatorOperationsNC_
    {
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);

        int unregisterUser(int id);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter);

        int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);

        int setTexture(int id, byte[] tex);
    }

    public interface ServerOperations_
    {
        void isRunning_async(Murmur.AMD_Server_isRunning cb__, Ice.Current current__);

        void start_async(Murmur.AMD_Server_start cb__, Ice.Current current__);

        void stop_async(Murmur.AMD_Server_stop cb__, Ice.Current current__);

        void delete_async(Murmur.AMD_Server_delete cb__, Ice.Current current__);

        void id_async(Murmur.AMD_Server_id cb__, Ice.Current current__);

        void addCallback_async(Murmur.AMD_Server_addCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__);

        void removeCallback_async(Murmur.AMD_Server_removeCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__);

        void setAuthenticator_async(Murmur.AMD_Server_setAuthenticator cb__, Murmur.ServerAuthenticatorPrx auth, Ice.Current current__);

        void getConf_async(Murmur.AMD_Server_getConf cb__, string key, Ice.Current current__);

        void getAllConf_async(Murmur.AMD_Server_getAllConf cb__, Ice.Current current__);

        void setConf_async(Murmur.AMD_Server_setConf cb__, string key, string value, Ice.Current current__);

        void setSuperuserPassword_async(Murmur.AMD_Server_setSuperuserPassword cb__, string pw, Ice.Current current__);

        void getLog_async(Murmur.AMD_Server_getLog cb__, int first, int last, Ice.Current current__);

        void getUsers_async(Murmur.AMD_Server_getUsers cb__, Ice.Current current__);

        void getChannels_async(Murmur.AMD_Server_getChannels cb__, Ice.Current current__);

        void getTree_async(Murmur.AMD_Server_getTree cb__, Ice.Current current__);

        void getBans_async(Murmur.AMD_Server_getBans cb__, Ice.Current current__);

        void setBans_async(Murmur.AMD_Server_setBans cb__, Murmur.Ban[] bans, Ice.Current current__);

        void kickUser_async(Murmur.AMD_Server_kickUser cb__, int session, string reason, Ice.Current current__);

        void getState_async(Murmur.AMD_Server_getState cb__, int session, Ice.Current current__);

        void setState_async(Murmur.AMD_Server_setState cb__, Murmur.User state, Ice.Current current__);

        void sendMessage_async(Murmur.AMD_Server_sendMessage cb__, int session, string text, Ice.Current current__);

        void hasPermission_async(Murmur.AMD_Server_hasPermission cb__, int session, int channelid, int perm, Ice.Current current__);

        void addContextCallback_async(Murmur.AMD_Server_addContextCallback cb__, int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, Ice.Current current__);

        void removeContextCallback_async(Murmur.AMD_Server_removeContextCallback cb__, Murmur.ServerContextCallbackPrx cb, Ice.Current current__);

        void getChannelState_async(Murmur.AMD_Server_getChannelState cb__, int channelid, Ice.Current current__);

        void setChannelState_async(Murmur.AMD_Server_setChannelState cb__, Murmur.Channel state, Ice.Current current__);

        void removeChannel_async(Murmur.AMD_Server_removeChannel cb__, int channelid, Ice.Current current__);

        void addChannel_async(Murmur.AMD_Server_addChannel cb__, string name, int parent, Ice.Current current__);

        void sendMessageChannel_async(Murmur.AMD_Server_sendMessageChannel cb__, int channelid, bool tree, string text, Ice.Current current__);

        void getACL_async(Murmur.AMD_Server_getACL cb__, int channelid, Ice.Current current__);

        void setACL_async(Murmur.AMD_Server_setACL cb__, int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, Ice.Current current__);

        void addUserToGroup_async(Murmur.AMD_Server_addUserToGroup cb__, int channelid, int session, string group, Ice.Current current__);

        void removeUserFromGroup_async(Murmur.AMD_Server_removeUserFromGroup cb__, int channelid, int session, string group, Ice.Current current__);

        void redirectWhisperGroup_async(Murmur.AMD_Server_redirectWhisperGroup cb__, int session, string source, string target, Ice.Current current__);

        void getUserNames_async(Murmur.AMD_Server_getUserNames cb__, int[] ids, Ice.Current current__);

        void getUserIds_async(Murmur.AMD_Server_getUserIds cb__, string[] names, Ice.Current current__);

        void registerUser_async(Murmur.AMD_Server_registerUser cb__, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        void unregisterUser_async(Murmur.AMD_Server_unregisterUser cb__, int userid, Ice.Current current__);

        void updateRegistration_async(Murmur.AMD_Server_updateRegistration cb__, int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        void getRegistration_async(Murmur.AMD_Server_getRegistration cb__, int userid, Ice.Current current__);

        void getRegisteredUsers_async(Murmur.AMD_Server_getRegisteredUsers cb__, string filter, Ice.Current current__);

        void verifyPassword_async(Murmur.AMD_Server_verifyPassword cb__, string name, string pw, Ice.Current current__);

        void getTexture_async(Murmur.AMD_Server_getTexture cb__, int userid, Ice.Current current__);

        void setTexture_async(Murmur.AMD_Server_setTexture cb__, int userid, byte[] tex, Ice.Current current__);
    }

    public interface ServerOperationsNC_
    {
        void isRunning_async(Murmur.AMD_Server_isRunning cb__);

        void start_async(Murmur.AMD_Server_start cb__);

        void stop_async(Murmur.AMD_Server_stop cb__);

        void delete_async(Murmur.AMD_Server_delete cb__);

        void id_async(Murmur.AMD_Server_id cb__);

        void addCallback_async(Murmur.AMD_Server_addCallback cb__, Murmur.ServerCallbackPrx cb);

        void removeCallback_async(Murmur.AMD_Server_removeCallback cb__, Murmur.ServerCallbackPrx cb);

        void setAuthenticator_async(Murmur.AMD_Server_setAuthenticator cb__, Murmur.ServerAuthenticatorPrx auth);

        void getConf_async(Murmur.AMD_Server_getConf cb__, string key);

        void getAllConf_async(Murmur.AMD_Server_getAllConf cb__);

        void setConf_async(Murmur.AMD_Server_setConf cb__, string key, string value);

        void setSuperuserPassword_async(Murmur.AMD_Server_setSuperuserPassword cb__, string pw);

        void getLog_async(Murmur.AMD_Server_getLog cb__, int first, int last);

        void getUsers_async(Murmur.AMD_Server_getUsers cb__);

        void getChannels_async(Murmur.AMD_Server_getChannels cb__);

        void getTree_async(Murmur.AMD_Server_getTree cb__);

        void getBans_async(Murmur.AMD_Server_getBans cb__);

        void setBans_async(Murmur.AMD_Server_setBans cb__, Murmur.Ban[] bans);

        void kickUser_async(Murmur.AMD_Server_kickUser cb__, int session, string reason);

        void getState_async(Murmur.AMD_Server_getState cb__, int session);

        void setState_async(Murmur.AMD_Server_setState cb__, Murmur.User state);

        void sendMessage_async(Murmur.AMD_Server_sendMessage cb__, int session, string text);

        void hasPermission_async(Murmur.AMD_Server_hasPermission cb__, int session, int channelid, int perm);

        void addContextCallback_async(Murmur.AMD_Server_addContextCallback cb__, int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx);

        void removeContextCallback_async(Murmur.AMD_Server_removeContextCallback cb__, Murmur.ServerContextCallbackPrx cb);

        void getChannelState_async(Murmur.AMD_Server_getChannelState cb__, int channelid);

        void setChannelState_async(Murmur.AMD_Server_setChannelState cb__, Murmur.Channel state);

        void removeChannel_async(Murmur.AMD_Server_removeChannel cb__, int channelid);

        void addChannel_async(Murmur.AMD_Server_addChannel cb__, string name, int parent);

        void sendMessageChannel_async(Murmur.AMD_Server_sendMessageChannel cb__, int channelid, bool tree, string text);

        void getACL_async(Murmur.AMD_Server_getACL cb__, int channelid);

        void setACL_async(Murmur.AMD_Server_setACL cb__, int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit);

        void addUserToGroup_async(Murmur.AMD_Server_addUserToGroup cb__, int channelid, int session, string group);

        void removeUserFromGroup_async(Murmur.AMD_Server_removeUserFromGroup cb__, int channelid, int session, string group);

        void redirectWhisperGroup_async(Murmur.AMD_Server_redirectWhisperGroup cb__, int session, string source, string target);

        void getUserNames_async(Murmur.AMD_Server_getUserNames cb__, int[] ids);

        void getUserIds_async(Murmur.AMD_Server_getUserIds cb__, string[] names);

        void registerUser_async(Murmur.AMD_Server_registerUser cb__, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);

        void unregisterUser_async(Murmur.AMD_Server_unregisterUser cb__, int userid);

        void updateRegistration_async(Murmur.AMD_Server_updateRegistration cb__, int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info);

        void getRegistration_async(Murmur.AMD_Server_getRegistration cb__, int userid);

        void getRegisteredUsers_async(Murmur.AMD_Server_getRegisteredUsers cb__, string filter);

        void verifyPassword_async(Murmur.AMD_Server_verifyPassword cb__, string name, string pw);

        void getTexture_async(Murmur.AMD_Server_getTexture cb__, int userid);

        void setTexture_async(Murmur.AMD_Server_setTexture cb__, int userid, byte[] tex);
    }

    public interface MetaCallbackOperations_
    {
        void started(Murmur.ServerPrx srv, Ice.Current current__);

        void stopped(Murmur.ServerPrx srv, Ice.Current current__);
    }

    public interface MetaCallbackOperationsNC_
    {
        void started(Murmur.ServerPrx srv);

        void stopped(Murmur.ServerPrx srv);
    }

    public interface MetaOperations_
    {
        void getServer_async(Murmur.AMD_Meta_getServer cb__, int id, Ice.Current current__);

        void newServer_async(Murmur.AMD_Meta_newServer cb__, Ice.Current current__);

        void getBootedServers_async(Murmur.AMD_Meta_getBootedServers cb__, Ice.Current current__);

        void getAllServers_async(Murmur.AMD_Meta_getAllServers cb__, Ice.Current current__);

        void getDefaultConf_async(Murmur.AMD_Meta_getDefaultConf cb__, Ice.Current current__);

        void getVersion_async(Murmur.AMD_Meta_getVersion cb__, Ice.Current current__);

        void addCallback_async(Murmur.AMD_Meta_addCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__);

        void removeCallback_async(Murmur.AMD_Meta_removeCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__);
    }

    public interface MetaOperationsNC_
    {
        void getServer_async(Murmur.AMD_Meta_getServer cb__, int id);

        void newServer_async(Murmur.AMD_Meta_newServer cb__);

        void getBootedServers_async(Murmur.AMD_Meta_getBootedServers cb__);

        void getAllServers_async(Murmur.AMD_Meta_getAllServers cb__);

        void getDefaultConf_async(Murmur.AMD_Meta_getDefaultConf cb__);

        void getVersion_async(Murmur.AMD_Meta_getVersion cb__);

        void addCallback_async(Murmur.AMD_Meta_addCallback cb__, Murmur.MetaCallbackPrx cb);

        void removeCallback_async(Murmur.AMD_Meta_removeCallback cb__, Murmur.MetaCallbackPrx cb);
    }
}

namespace Murmur
{
    public sealed class NetAddressHelper
    {
        public static void write(IceInternal.BasicStream os__, byte[] v__)
        {
            os__.writeByteSeq(v__);
        }

        public static byte[] read(IceInternal.BasicStream is__)
        {
            byte[] v__;
            v__ = is__.readByteSeq();
            return v__;
        }
    }

    public sealed class IntListHelper
    {
        public static void write(IceInternal.BasicStream os__, int[] v__)
        {
            os__.writeIntSeq(v__);
        }

        public static int[] read(IceInternal.BasicStream is__)
        {
            int[] v__;
            v__ = is__.readIntSeq();
            return v__;
        }
    }

    public sealed class TreeListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.Tree[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    os__.writeObject(v__[ix__]);
                }
            }
        }

        public static Murmur.Tree[] read(IceInternal.BasicStream is__)
        {
            Murmur.Tree[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 4);
                v__ = new Murmur.Tree[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    IceInternal.ArrayPatcher<Murmur.Tree> spx = new IceInternal.ArrayPatcher<Murmur.Tree>("::Murmur::Tree", v__, ix__);
                    is__.readObject(spx);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class UserMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<int, Murmur.User> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<int, Murmur.User> e__ in v__)
                {
                    os__.writeInt(e__.Key);
                    if(e__.Value == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        e__.Value.write__(os__);
                    }
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<int, Murmur.User> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<int, Murmur.User> r__ = new _System.Collections.Generic.Dictionary<int, Murmur.User>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                int k__;
                k__ = is__.readInt();
                Murmur.User v__;
                v__ = null;
                if(v__ == null)
                {
                    v__ = new Murmur.User();
                }
                v__.read__(is__);
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class ChannelMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<int, Murmur.Channel> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<int, Murmur.Channel> e__ in v__)
                {
                    os__.writeInt(e__.Key);
                    if(e__.Value == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        e__.Value.write__(os__);
                    }
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<int, Murmur.Channel> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<int, Murmur.Channel> r__ = new _System.Collections.Generic.Dictionary<int, Murmur.Channel>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                int k__;
                k__ = is__.readInt();
                Murmur.Channel v__;
                v__ = null;
                if(v__ == null)
                {
                    v__ = new Murmur.Channel();
                }
                v__.read__(is__);
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class ChannelListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.Channel[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.Channel() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.Channel[] read(IceInternal.BasicStream is__)
        {
            Murmur.Channel[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 16);
                v__ = new Murmur.Channel[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.Channel();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class UserListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.User[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.User() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.User[] read(IceInternal.BasicStream is__)
        {
            Murmur.User[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 42);
                v__ = new Murmur.User[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.User();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class GroupListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.Group[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.Group() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.Group[] read(IceInternal.BasicStream is__)
        {
            Murmur.Group[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 7);
                v__ = new Murmur.Group[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.Group();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class ACLListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.ACL[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.ACL() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.ACL[] read(IceInternal.BasicStream is__)
        {
            Murmur.ACL[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 16);
                v__ = new Murmur.ACL[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.ACL();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class LogListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.LogEntry[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.LogEntry() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.LogEntry[] read(IceInternal.BasicStream is__)
        {
            Murmur.LogEntry[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 5);
                v__ = new Murmur.LogEntry[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.LogEntry();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class BanListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.Ban[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    (v__[ix__] == null ? new Murmur.Ban() : v__[ix__]).write__(os__);
                }
            }
        }

        public static Murmur.Ban[] read(IceInternal.BasicStream is__)
        {
            Murmur.Ban[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 20);
                v__ = new Murmur.Ban[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = new Murmur.Ban();
                    v__[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class IdListHelper
    {
        public static void write(IceInternal.BasicStream os__, int[] v__)
        {
            os__.writeIntSeq(v__);
        }

        public static int[] read(IceInternal.BasicStream is__)
        {
            int[] v__;
            v__ = is__.readIntSeq();
            return v__;
        }
    }

    public sealed class NameListHelper
    {
        public static void write(IceInternal.BasicStream os__, string[] v__)
        {
            os__.writeStringSeq(v__);
        }

        public static string[] read(IceInternal.BasicStream is__)
        {
            string[] v__;
            v__ = is__.readStringSeq();
            return v__;
        }
    }

    public sealed class NameMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<int, string> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<int, string> e__ in v__)
                {
                    os__.writeInt(e__.Key);
                    os__.writeString(e__.Value);
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<int, string> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<int, string> r__ = new _System.Collections.Generic.Dictionary<int, string>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                int k__;
                k__ = is__.readInt();
                string v__;
                v__ = is__.readString();
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class IdMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<string, int> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<string, int> e__ in v__)
                {
                    os__.writeString(e__.Key);
                    os__.writeInt(e__.Value);
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<string, int> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<string, int> r__ = new _System.Collections.Generic.Dictionary<string, int>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                string k__;
                k__ = is__.readString();
                int v__;
                v__ = is__.readInt();
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class TextureHelper
    {
        public static void write(IceInternal.BasicStream os__, byte[] v__)
        {
            os__.writeByteSeq(v__);
        }

        public static byte[] read(IceInternal.BasicStream is__)
        {
            byte[] v__;
            v__ = is__.readByteSeq();
            return v__;
        }
    }

    public sealed class ConfigMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<string, string> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<string, string> e__ in v__)
                {
                    os__.writeString(e__.Key);
                    os__.writeString(e__.Value);
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<string, string> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<string, string> r__ = new _System.Collections.Generic.Dictionary<string, string>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                string k__;
                k__ = is__.readString();
                string v__;
                v__ = is__.readString();
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class GroupNameListHelper
    {
        public static void write(IceInternal.BasicStream os__, string[] v__)
        {
            os__.writeStringSeq(v__);
        }

        public static string[] read(IceInternal.BasicStream is__)
        {
            string[] v__;
            v__ = is__.readStringSeq();
            return v__;
        }
    }

    public sealed class CertificateDerHelper
    {
        public static void write(IceInternal.BasicStream os__, byte[] v__)
        {
            os__.writeByteSeq(v__);
        }

        public static byte[] read(IceInternal.BasicStream is__)
        {
            byte[] v__;
            v__ = is__.readByteSeq();
            return v__;
        }
    }

    public sealed class CertificateListHelper
    {
        public static void write(IceInternal.BasicStream os__, byte[][] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    Murmur.CertificateDerHelper.write(os__, v__[ix__]);
                }
            }
        }

        public static byte[][] read(IceInternal.BasicStream is__)
        {
            byte[][] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 1);
                v__ = new byte[szx__][];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = Murmur.CertificateDerHelper.read(is__);
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class UserInfoMapHelper
    {
        public static void write(IceInternal.BasicStream os__,
                                 _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Count);
                foreach(_System.Collections.Generic.KeyValuePair<Murmur.UserInfo, string> e__ in v__)
                {
                    os__.writeByte((byte)e__.Key, 5);
                    os__.writeString(e__.Value);
                }
            }
        }

        public static _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> read(IceInternal.BasicStream is__)
        {
            int sz__ = is__.readSize();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> r__ = new _System.Collections.Generic.Dictionary<Murmur.UserInfo, string>();
            for(int i__ = 0; i__ < sz__; ++i__)
            {
                Murmur.UserInfo k__;
                k__ = (Murmur.UserInfo)is__.readByte(5);
                string v__;
                v__ = is__.readString();
                r__[k__] = v__;
            }
            return r__;
        }
    }

    public sealed class TreePrxHelper : Ice.ObjectPrxHelperBase, TreePrx
    {
        #region Checked and unchecked cast operations

        public static TreePrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            TreePrx r = b as TreePrx;
            if((r == null) && b.ice_isA("::Murmur::Tree"))
            {
                TreePrxHelper h = new TreePrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static TreePrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            TreePrx r = b as TreePrx;
            if((r == null) && b.ice_isA("::Murmur::Tree", ctx))
            {
                TreePrxHelper h = new TreePrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static TreePrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Tree"))
                {
                    TreePrxHelper h = new TreePrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static TreePrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Tree", ctx))
                {
                    TreePrxHelper h = new TreePrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static TreePrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            TreePrx r = b as TreePrx;
            if(r == null)
            {
                TreePrxHelper h = new TreePrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static TreePrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            TreePrxHelper h = new TreePrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new TreeDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new TreeDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, TreePrx v__)
        {
            os__.writeProxy(v__);
        }

        public static TreePrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                TreePrxHelper result = new TreePrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerCallbackPrxHelper : Ice.ObjectPrxHelperBase, ServerCallbackPrx
    {
        #region Synchronous operations

        public void channelCreated(Murmur.Channel state)
        {
            channelCreated(state, null, false);
        }

        public void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            channelCreated(state, context__, true);
        }

        private void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.channelCreated(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void channelRemoved(Murmur.Channel state)
        {
            channelRemoved(state, null, false);
        }

        public void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            channelRemoved(state, context__, true);
        }

        private void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.channelRemoved(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void channelStateChanged(Murmur.Channel state)
        {
            channelStateChanged(state, null, false);
        }

        public void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            channelStateChanged(state, context__, true);
        }

        private void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.channelStateChanged(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void userConnected(Murmur.User state)
        {
            userConnected(state, null, false);
        }

        public void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            userConnected(state, context__, true);
        }

        private void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.userConnected(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void userDisconnected(Murmur.User state)
        {
            userDisconnected(state, null, false);
        }

        public void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            userDisconnected(state, context__, true);
        }

        private void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.userDisconnected(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void userStateChanged(Murmur.User state)
        {
            userStateChanged(state, null, false);
        }

        public void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            userStateChanged(state, context__, true);
        }

        private void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerCallbackDel_ del__ = (ServerCallbackDel_)delBase__;
                    del__.userStateChanged(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static ServerCallbackPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerCallbackPrx r = b as ServerCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerCallback"))
            {
                ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerCallbackPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            ServerCallbackPrx r = b as ServerCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerCallback", ctx))
            {
                ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerCallbackPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerCallback"))
                {
                    ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerCallbackPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerCallback", ctx))
                {
                    ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerCallbackPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerCallbackPrx r = b as ServerCallbackPrx;
            if(r == null)
            {
                ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerCallbackPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            ServerCallbackPrxHelper h = new ServerCallbackPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new ServerCallbackDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new ServerCallbackDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, ServerCallbackPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static ServerCallbackPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                ServerCallbackPrxHelper result = new ServerCallbackPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerContextCallbackPrxHelper : Ice.ObjectPrxHelperBase, ServerContextCallbackPrx
    {
        #region Synchronous operations

        public void contextAction(string action, Murmur.User usr, int session, int channelid)
        {
            contextAction(action, usr, session, channelid, null, false);
        }

        public void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            contextAction(action, usr, session, channelid, context__, true);
        }

        private void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerContextCallbackDel_ del__ = (ServerContextCallbackDel_)delBase__;
                    del__.contextAction(action, usr, session, channelid, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static ServerContextCallbackPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerContextCallbackPrx r = b as ServerContextCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerContextCallback"))
            {
                ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerContextCallbackPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            ServerContextCallbackPrx r = b as ServerContextCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerContextCallback", ctx))
            {
                ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerContextCallbackPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerContextCallback"))
                {
                    ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerContextCallbackPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerContextCallback", ctx))
                {
                    ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerContextCallbackPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerContextCallbackPrx r = b as ServerContextCallbackPrx;
            if(r == null)
            {
                ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerContextCallbackPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            ServerContextCallbackPrxHelper h = new ServerContextCallbackPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new ServerContextCallbackDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new ServerContextCallbackDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, ServerContextCallbackPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static ServerContextCallbackPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                ServerContextCallbackPrxHelper result = new ServerContextCallbackPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerAuthenticatorPrxHelper : Ice.ObjectPrxHelperBase, ServerAuthenticatorPrx
    {
        #region Synchronous operations

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, null, false);
        }

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, context__, true);
        }

        private int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("authenticate");
                    delBase__ = getDelegate__(false);
                    ServerAuthenticatorDel_ del__ = (ServerAuthenticatorDel_)delBase__;
                    return del__.authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return getInfo(id, out info, null, false);
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getInfo(id, out info, context__, true);
        }

        private bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getInfo");
                    delBase__ = getDelegate__(false);
                    ServerAuthenticatorDel_ del__ = (ServerAuthenticatorDel_)delBase__;
                    return del__.getInfo(id, out info, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public string idToName(int id)
        {
            return idToName(id, null, false);
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return idToName(id, context__, true);
        }

        private string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("idToName");
                    delBase__ = getDelegate__(false);
                    ServerAuthenticatorDel_ del__ = (ServerAuthenticatorDel_)delBase__;
                    return del__.idToName(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public byte[] idToTexture(int id)
        {
            return idToTexture(id, null, false);
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return idToTexture(id, context__, true);
        }

        private byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("idToTexture");
                    delBase__ = getDelegate__(false);
                    ServerAuthenticatorDel_ del__ = (ServerAuthenticatorDel_)delBase__;
                    return del__.idToTexture(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int nameToId(string name)
        {
            return nameToId(name, null, false);
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return nameToId(name, context__, true);
        }

        private int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("nameToId");
                    delBase__ = getDelegate__(false);
                    ServerAuthenticatorDel_ del__ = (ServerAuthenticatorDel_)delBase__;
                    return del__.nameToId(name, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static ServerAuthenticatorPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerAuthenticatorPrx r = b as ServerAuthenticatorPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerAuthenticator"))
            {
                ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerAuthenticatorPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            ServerAuthenticatorPrx r = b as ServerAuthenticatorPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerAuthenticator", ctx))
            {
                ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerAuthenticatorPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerAuthenticator"))
                {
                    ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerAuthenticatorPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerAuthenticator", ctx))
                {
                    ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerAuthenticatorPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerAuthenticatorPrx r = b as ServerAuthenticatorPrx;
            if(r == null)
            {
                ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerAuthenticatorPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            ServerAuthenticatorPrxHelper h = new ServerAuthenticatorPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new ServerAuthenticatorDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new ServerAuthenticatorDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, ServerAuthenticatorPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static ServerAuthenticatorPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                ServerAuthenticatorPrxHelper result = new ServerAuthenticatorPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerUpdatingAuthenticatorPrxHelper : Ice.ObjectPrxHelperBase, ServerUpdatingAuthenticatorPrx
    {
        #region Synchronous operations

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, null, false);
        }

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, context__, true);
        }

        private int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("authenticate");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return getInfo(id, out info, null, false);
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getInfo(id, out info, context__, true);
        }

        private bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getInfo");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.getInfo(id, out info, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public string idToName(int id)
        {
            return idToName(id, null, false);
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return idToName(id, context__, true);
        }

        private string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("idToName");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.idToName(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public byte[] idToTexture(int id)
        {
            return idToTexture(id, null, false);
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return idToTexture(id, context__, true);
        }

        private byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("idToTexture");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.idToTexture(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int nameToId(string name)
        {
            return nameToId(name, null, false);
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return nameToId(name, context__, true);
        }

        private int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("nameToId");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.nameToId(name, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter)
        {
            return getRegisteredUsers(filter, null, false);
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getRegisteredUsers(filter, context__, true);
        }

        private _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getRegisteredUsers");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.getRegisteredUsers(filter, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return registerUser(info, null, false);
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return registerUser(info, context__, true);
        }

        private int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("registerUser");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.registerUser(info, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return setInfo(id, info, null, false);
        }

        public int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return setInfo(id, info, context__, true);
        }

        private int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setInfo");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.setInfo(id, info, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int setTexture(int id, byte[] tex)
        {
            return setTexture(id, tex, null, false);
        }

        public int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return setTexture(id, tex, context__, true);
        }

        private int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setTexture");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.setTexture(id, tex, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int unregisterUser(int id)
        {
            return unregisterUser(id, null, false);
        }

        public int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return unregisterUser(id, context__, true);
        }

        private int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("unregisterUser");
                    delBase__ = getDelegate__(false);
                    ServerUpdatingAuthenticatorDel_ del__ = (ServerUpdatingAuthenticatorDel_)delBase__;
                    return del__.unregisterUser(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static ServerUpdatingAuthenticatorPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerUpdatingAuthenticatorPrx r = b as ServerUpdatingAuthenticatorPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerUpdatingAuthenticator"))
            {
                ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerUpdatingAuthenticatorPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            ServerUpdatingAuthenticatorPrx r = b as ServerUpdatingAuthenticatorPrx;
            if((r == null) && b.ice_isA("::Murmur::ServerUpdatingAuthenticator", ctx))
            {
                ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerUpdatingAuthenticatorPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerUpdatingAuthenticator"))
                {
                    ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerUpdatingAuthenticatorPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::ServerUpdatingAuthenticator", ctx))
                {
                    ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerUpdatingAuthenticatorPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerUpdatingAuthenticatorPrx r = b as ServerUpdatingAuthenticatorPrx;
            if(r == null)
            {
                ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerUpdatingAuthenticatorPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            ServerUpdatingAuthenticatorPrxHelper h = new ServerUpdatingAuthenticatorPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new ServerUpdatingAuthenticatorDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new ServerUpdatingAuthenticatorDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, ServerUpdatingAuthenticatorPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static ServerUpdatingAuthenticatorPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                ServerUpdatingAuthenticatorPrxHelper result = new ServerUpdatingAuthenticatorPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerPrxHelper : Ice.ObjectPrxHelperBase, ServerPrx
    {
        #region Synchronous operations

        public void addCallback(Murmur.ServerCallbackPrx cb)
        {
            addCallback(cb, null, false);
        }

        public void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            addCallback(cb, context__, true);
        }

        private void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("addCallback");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.addCallback(cb, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int addChannel(string name, int parent)
        {
            return addChannel(name, parent, null, false);
        }

        public int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return addChannel(name, parent, context__, true);
        }

        private int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("addChannel");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.addChannel(name, parent, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx)
        {
            addContextCallback(session, action, text, cb, ctx, null, false);
        }

        public void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            addContextCallback(session, action, text, cb, ctx, context__, true);
        }

        private void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("addContextCallback");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.addContextCallback(session, action, text, cb, ctx, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void addUserToGroup(int channelid, int session, string group)
        {
            addUserToGroup(channelid, session, group, null, false);
        }

        public void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            addUserToGroup(channelid, session, group, context__, true);
        }

        private void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("addUserToGroup");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.addUserToGroup(channelid, session, group, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void delete()
        {
            delete(null, false);
        }

        public void delete(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            delete(context__, true);
        }

        private void delete(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("delete");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.delete(context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit)
        {
            getACL(channelid, out acls, out groups, out inherit, null, false);
        }

        public void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            getACL(channelid, out acls, out groups, out inherit, context__, true);
        }

        private void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getACL");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.getACL(channelid, out acls, out groups, out inherit, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<string, string> getAllConf()
        {
            return getAllConf(null, false);
        }

        public _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getAllConf(context__, true);
        }

        private _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getAllConf");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getAllConf(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.Ban[] getBans()
        {
            return getBans(null, false);
        }

        public Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getBans(context__, true);
        }

        private Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getBans");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getBans(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.Channel getChannelState(int channelid)
        {
            return getChannelState(channelid, null, false);
        }

        public Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getChannelState(channelid, context__, true);
        }

        private Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getChannelState");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getChannelState(channelid, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels()
        {
            return getChannels(null, false);
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getChannels(context__, true);
        }

        private _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getChannels");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getChannels(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public string getConf(string key)
        {
            return getConf(key, null, false);
        }

        public string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getConf(key, context__, true);
        }

        private string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getConf");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getConf(key, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.LogEntry[] getLog(int first, int last)
        {
            return getLog(first, last, null, false);
        }

        public Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getLog(first, last, context__, true);
        }

        private Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getLog");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getLog(first, last, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter)
        {
            return getRegisteredUsers(filter, null, false);
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getRegisteredUsers(filter, context__, true);
        }

        private _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getRegisteredUsers");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getRegisteredUsers(filter, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid)
        {
            return getRegistration(userid, null, false);
        }

        public _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getRegistration(userid, context__, true);
        }

        private _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getRegistration");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getRegistration(userid, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.User getState(int session)
        {
            return getState(session, null, false);
        }

        public Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getState(session, context__, true);
        }

        private Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getState");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getState(session, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public byte[] getTexture(int userid)
        {
            return getTexture(userid, null, false);
        }

        public byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getTexture(userid, context__, true);
        }

        private byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getTexture");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getTexture(userid, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.Tree getTree()
        {
            return getTree(null, false);
        }

        public Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getTree(context__, true);
        }

        private Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getTree");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getTree(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names)
        {
            return getUserIds(names, null, false);
        }

        public _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getUserIds(names, context__, true);
        }

        private _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getUserIds");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getUserIds(names, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids)
        {
            return getUserNames(ids, null, false);
        }

        public _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getUserNames(ids, context__, true);
        }

        private _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getUserNames");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getUserNames(ids, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers()
        {
            return getUsers(null, false);
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getUsers(context__, true);
        }

        private _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getUsers");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.getUsers(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public bool hasPermission(int session, int channelid, int perm)
        {
            return hasPermission(session, channelid, perm, null, false);
        }

        public bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return hasPermission(session, channelid, perm, context__, true);
        }

        private bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("hasPermission");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.hasPermission(session, channelid, perm, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int id()
        {
            return id(null, false);
        }

        public int id(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return id(context__, true);
        }

        private int id(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("id");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.id(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public bool isRunning()
        {
            return isRunning(null, false);
        }

        public bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return isRunning(context__, true);
        }

        private bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("isRunning");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.isRunning(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void kickUser(int session, string reason)
        {
            kickUser(session, reason, null, false);
        }

        public void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            kickUser(session, reason, context__, true);
        }

        private void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("kickUser");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.kickUser(session, reason, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void redirectWhisperGroup(int session, string source, string target)
        {
            redirectWhisperGroup(session, source, target, null, false);
        }

        public void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            redirectWhisperGroup(session, source, target, context__, true);
        }

        private void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("redirectWhisperGroup");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.redirectWhisperGroup(session, source, target, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return registerUser(info, null, false);
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return registerUser(info, context__, true);
        }

        private int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("registerUser");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.registerUser(info, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void removeCallback(Murmur.ServerCallbackPrx cb)
        {
            removeCallback(cb, null, false);
        }

        public void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            removeCallback(cb, context__, true);
        }

        private void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("removeCallback");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.removeCallback(cb, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void removeChannel(int channelid)
        {
            removeChannel(channelid, null, false);
        }

        public void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            removeChannel(channelid, context__, true);
        }

        private void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("removeChannel");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.removeChannel(channelid, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void removeContextCallback(Murmur.ServerContextCallbackPrx cb)
        {
            removeContextCallback(cb, null, false);
        }

        public void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            removeContextCallback(cb, context__, true);
        }

        private void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("removeContextCallback");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.removeContextCallback(cb, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void removeUserFromGroup(int channelid, int session, string group)
        {
            removeUserFromGroup(channelid, session, group, null, false);
        }

        public void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            removeUserFromGroup(channelid, session, group, context__, true);
        }

        private void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("removeUserFromGroup");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.removeUserFromGroup(channelid, session, group, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void sendMessage(int session, string text)
        {
            sendMessage(session, text, null, false);
        }

        public void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            sendMessage(session, text, context__, true);
        }

        private void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("sendMessage");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.sendMessage(session, text, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void sendMessageChannel(int channelid, bool tree, string text)
        {
            sendMessageChannel(channelid, tree, text, null, false);
        }

        public void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            sendMessageChannel(channelid, tree, text, context__, true);
        }

        private void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("sendMessageChannel");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.sendMessageChannel(channelid, tree, text, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit)
        {
            setACL(channelid, acls, groups, inherit, null, false);
        }

        public void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setACL(channelid, acls, groups, inherit, context__, true);
        }

        private void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setACL");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setACL(channelid, acls, groups, inherit, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setAuthenticator(Murmur.ServerAuthenticatorPrx auth)
        {
            setAuthenticator(auth, null, false);
        }

        public void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setAuthenticator(auth, context__, true);
        }

        private void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setAuthenticator");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setAuthenticator(auth, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setBans(Murmur.Ban[] bans)
        {
            setBans(bans, null, false);
        }

        public void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setBans(bans, context__, true);
        }

        private void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setBans");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setBans(bans, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setChannelState(Murmur.Channel state)
        {
            setChannelState(state, null, false);
        }

        public void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setChannelState(state, context__, true);
        }

        private void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setChannelState");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setChannelState(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setConf(string key, string value)
        {
            setConf(key, value, null, false);
        }

        public void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setConf(key, value, context__, true);
        }

        private void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setConf(key, value, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setState(Murmur.User state)
        {
            setState(state, null, false);
        }

        public void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setState(state, context__, true);
        }

        private void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setState");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setState(state, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setSuperuserPassword(string pw)
        {
            setSuperuserPassword(pw, null, false);
        }

        public void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setSuperuserPassword(pw, context__, true);
        }

        private void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setSuperuserPassword(pw, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void setTexture(int userid, byte[] tex)
        {
            setTexture(userid, tex, null, false);
        }

        public void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            setTexture(userid, tex, context__, true);
        }

        private void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("setTexture");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.setTexture(userid, tex, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void start()
        {
            start(null, false);
        }

        public void start(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            start(context__, true);
        }

        private void start(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("start");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.start(context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void stop()
        {
            stop(null, false);
        }

        public void stop(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            stop(context__, true);
        }

        private void stop(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("stop");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.stop(context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void unregisterUser(int userid)
        {
            unregisterUser(userid, null, false);
        }

        public void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            unregisterUser(userid, context__, true);
        }

        private void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("unregisterUser");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.unregisterUser(userid, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            updateRegistration(userid, info, null, false);
        }

        public void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            updateRegistration(userid, info, context__, true);
        }

        private void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("updateRegistration");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    del__.updateRegistration(userid, info, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public int verifyPassword(string name, string pw)
        {
            return verifyPassword(name, pw, null, false);
        }

        public int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return verifyPassword(name, pw, context__, true);
        }

        private int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("verifyPassword");
                    delBase__ = getDelegate__(false);
                    ServerDel_ del__ = (ServerDel_)delBase__;
                    return del__.verifyPassword(name, pw, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static ServerPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerPrx r = b as ServerPrx;
            if((r == null) && b.ice_isA("::Murmur::Server"))
            {
                ServerPrxHelper h = new ServerPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            ServerPrx r = b as ServerPrx;
            if((r == null) && b.ice_isA("::Murmur::Server", ctx))
            {
                ServerPrxHelper h = new ServerPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Server"))
                {
                    ServerPrxHelper h = new ServerPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Server", ctx))
                {
                    ServerPrxHelper h = new ServerPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static ServerPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            ServerPrx r = b as ServerPrx;
            if(r == null)
            {
                ServerPrxHelper h = new ServerPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static ServerPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            ServerPrxHelper h = new ServerPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new ServerDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new ServerDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, ServerPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static ServerPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                ServerPrxHelper result = new ServerPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class MetaCallbackPrxHelper : Ice.ObjectPrxHelperBase, MetaCallbackPrx
    {
        #region Synchronous operations

        public void started(Murmur.ServerPrx srv)
        {
            started(srv, null, false);
        }

        public void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            started(srv, context__, true);
        }

        private void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    MetaCallbackDel_ del__ = (MetaCallbackDel_)delBase__;
                    del__.started(srv, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void stopped(Murmur.ServerPrx srv)
        {
            stopped(srv, null, false);
        }

        public void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            stopped(srv, context__, true);
        }

        private void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    delBase__ = getDelegate__(false);
                    MetaCallbackDel_ del__ = (MetaCallbackDel_)delBase__;
                    del__.stopped(srv, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static MetaCallbackPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            MetaCallbackPrx r = b as MetaCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::MetaCallback"))
            {
                MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaCallbackPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            MetaCallbackPrx r = b as MetaCallbackPrx;
            if((r == null) && b.ice_isA("::Murmur::MetaCallback", ctx))
            {
                MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaCallbackPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::MetaCallback"))
                {
                    MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static MetaCallbackPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::MetaCallback", ctx))
                {
                    MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static MetaCallbackPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            MetaCallbackPrx r = b as MetaCallbackPrx;
            if(r == null)
            {
                MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaCallbackPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            MetaCallbackPrxHelper h = new MetaCallbackPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new MetaCallbackDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new MetaCallbackDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, MetaCallbackPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static MetaCallbackPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                MetaCallbackPrxHelper result = new MetaCallbackPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }

    public sealed class ServerListHelper
    {
        public static void write(IceInternal.BasicStream os__, Murmur.ServerPrx[] v__)
        {
            if(v__ == null)
            {
                os__.writeSize(0);
            }
            else
            {
                os__.writeSize(v__.Length);
                for(int ix__ = 0; ix__ < v__.Length; ++ix__)
                {
                    Murmur.ServerPrxHelper.write__(os__, v__[ix__]);
                }
            }
        }

        public static Murmur.ServerPrx[] read(IceInternal.BasicStream is__)
        {
            Murmur.ServerPrx[] v__;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 2);
                v__ = new Murmur.ServerPrx[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    v__[ix__] = Murmur.ServerPrxHelper.read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            return v__;
        }
    }

    public sealed class MetaPrxHelper : Ice.ObjectPrxHelperBase, MetaPrx
    {
        #region Synchronous operations

        public void addCallback(Murmur.MetaCallbackPrx cb)
        {
            addCallback(cb, null, false);
        }

        public void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            addCallback(cb, context__, true);
        }

        private void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("addCallback");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    del__.addCallback(cb, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.ServerPrx[] getAllServers()
        {
            return getAllServers(null, false);
        }

        public Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getAllServers(context__, true);
        }

        private Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getAllServers");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    return del__.getAllServers(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.ServerPrx[] getBootedServers()
        {
            return getBootedServers(null, false);
        }

        public Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getBootedServers(context__, true);
        }

        private Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getBootedServers");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    return del__.getBootedServers(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public _System.Collections.Generic.Dictionary<string, string> getDefaultConf()
        {
            return getDefaultConf(null, false);
        }

        public _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getDefaultConf(context__, true);
        }

        private _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getDefaultConf");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    return del__.getDefaultConf(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.ServerPrx getServer(int id)
        {
            return getServer(id, null, false);
        }

        public Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            return getServer(id, context__, true);
        }

        private Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getServer");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    return del__.getServer(id, context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void getVersion(out int major, out int minor, out int patch, out string text)
        {
            getVersion(out major, out minor, out patch, out text, null, false);
        }

        public void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            getVersion(out major, out minor, out patch, out text, context__, true);
        }

        private void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("getVersion");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    del__.getVersion(out major, out minor, out patch, out text, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapperRelaxed__(delBase__, ex__, null, ref cnt__);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public Murmur.ServerPrx newServer()
        {
            return newServer(null, false);
        }

        public Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            return newServer(context__, true);
        }

        private Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("newServer");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    return del__.newServer(context__);
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        public void removeCallback(Murmur.MetaCallbackPrx cb)
        {
            removeCallback(cb, null, false);
        }

        public void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            removeCallback(cb, context__, true);
        }

        private void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__, bool explicitContext__)
        {
            if(explicitContext__ && context__ == null)
            {
                context__ = emptyContext_;
            }
            int cnt__ = 0;
            while(true)
            {
                Ice.ObjectDel_ delBase__ = null;
                try
                {
                    checkTwowayOnly__("removeCallback");
                    delBase__ = getDelegate__(false);
                    MetaDel_ del__ = (MetaDel_)delBase__;
                    del__.removeCallback(cb, context__);
                    return;
                }
                catch(IceInternal.LocalExceptionWrapper ex__)
                {
                    handleExceptionWrapper__(delBase__, ex__, null);
                }
                catch(Ice.LocalException ex__)
                {
                    handleException__(delBase__, ex__, null, ref cnt__);
                }
            }
        }

        #endregion

        #region Checked and unchecked cast operations

        public static MetaPrx checkedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            MetaPrx r = b as MetaPrx;
            if((r == null) && b.ice_isA("::Murmur::Meta"))
            {
                MetaPrxHelper h = new MetaPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaPrx checkedCast(Ice.ObjectPrx b, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            MetaPrx r = b as MetaPrx;
            if((r == null) && b.ice_isA("::Murmur::Meta", ctx))
            {
                MetaPrxHelper h = new MetaPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaPrx checkedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Meta"))
                {
                    MetaPrxHelper h = new MetaPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static MetaPrx checkedCast(Ice.ObjectPrx b, string f, _System.Collections.Generic.Dictionary<string, string> ctx)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            try
            {
                if(bb.ice_isA("::Murmur::Meta", ctx))
                {
                    MetaPrxHelper h = new MetaPrxHelper();
                    h.copyFrom__(bb);
                    return h;
                }
            }
            catch(Ice.FacetNotExistException)
            {
            }
            return null;
        }

        public static MetaPrx uncheckedCast(Ice.ObjectPrx b)
        {
            if(b == null)
            {
                return null;
            }
            MetaPrx r = b as MetaPrx;
            if(r == null)
            {
                MetaPrxHelper h = new MetaPrxHelper();
                h.copyFrom__(b);
                r = h;
            }
            return r;
        }

        public static MetaPrx uncheckedCast(Ice.ObjectPrx b, string f)
        {
            if(b == null)
            {
                return null;
            }
            Ice.ObjectPrx bb = b.ice_facet(f);
            MetaPrxHelper h = new MetaPrxHelper();
            h.copyFrom__(bb);
            return h;
        }

        #endregion

        #region Marshaling support

        protected override Ice.ObjectDelM_ createDelegateM__()
        {
            return new MetaDelM_();
        }

        protected override Ice.ObjectDelD_ createDelegateD__()
        {
            return new MetaDelD_();
        }

        public static void write__(IceInternal.BasicStream os__, MetaPrx v__)
        {
            os__.writeProxy(v__);
        }

        public static MetaPrx read__(IceInternal.BasicStream is__)
        {
            Ice.ObjectPrx proxy = is__.readProxy();
            if(proxy != null)
            {
                MetaPrxHelper result = new MetaPrxHelper();
                result.copyFrom__(proxy);
                return result;
            }
            return null;
        }

        #endregion
    }
}

namespace Murmur
{
    public interface TreeDel_ : Ice.ObjectDel_
    {
    }

    public interface ServerCallbackDel_ : Ice.ObjectDel_
    {
        void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerContextCallbackDel_ : Ice.ObjectDel_
    {
        void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerAuthenticatorDel_ : Ice.ObjectDel_
    {
        int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__);

        bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__);

        string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerUpdatingAuthenticatorDel_ : Murmur.ServerAuthenticatorDel_
    {
        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__);

        int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface ServerDel_ : Ice.ObjectDel_
    {
        bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__);

        void start(_System.Collections.Generic.Dictionary<string, string> context__);

        void stop(_System.Collections.Generic.Dictionary<string, string> context__);

        void delete(_System.Collections.Generic.Dictionary<string, string> context__);

        int id(_System.Collections.Generic.Dictionary<string, string> context__);

        void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__);

        string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__);

        void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__);

        void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__);

        void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__);

        void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__);

        void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__);

        void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__);

        bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__);

        void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__);

        void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__);

        int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__);

        void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__);

        void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__);

        void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__);

        void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__);

        void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__);

        int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__);

        int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__);

        byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__);

        void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface MetaCallbackDel_ : Ice.ObjectDel_
    {
        void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__);

        void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__);
    }

    public interface MetaDel_ : Ice.ObjectDel_
    {
        Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__);

        Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__);

        _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__);

        void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__);

        void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);

        void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__);
    }
}

namespace Murmur
{
    public sealed class TreeDelM_ : Ice.ObjectDelM_, TreeDel_
    {
    }

    public sealed class ServerCallbackDelM_ : Ice.ObjectDelM_, ServerCallbackDel_
    {
        public void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("channelCreated", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("channelRemoved", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("channelStateChanged", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("userConnected", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("userDisconnected", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("userStateChanged", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class ServerContextCallbackDelM_ : Ice.ObjectDelM_, ServerContextCallbackDel_
    {
        public void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("contextAction", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(action);
                    if(usr == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        usr.write__(os__);
                    }
                    os__.writeInt(session);
                    os__.writeInt(channelid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class ServerAuthenticatorDelM_ : Ice.ObjectDelM_, ServerAuthenticatorDel_
    {
        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("authenticate", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                    os__.writeString(pw);
                    if(certificates == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(certificates.Length);
                        for(int ix__ = 0; ix__ < certificates.Length; ++ix__)
                        {
                            Murmur.CertificateDerHelper.write(os__, certificates[ix__]);
                        }
                    }
                    os__.writeString(certhash);
                    os__.writeBool(certstrong);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    newname = is__.readString();
                    groups = is__.readStringSeq();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getInfo", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    info = Murmur.UserInfoMapHelper.read(is__);
                    bool ret__;
                    ret__ = is__.readBool();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("idToName", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    string ret__;
                    ret__ = is__.readString();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("idToTexture", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    byte[] ret__;
                    ret__ = is__.readByteSeq();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("nameToId", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class ServerUpdatingAuthenticatorDelM_ : Ice.ObjectDelM_, ServerUpdatingAuthenticatorDel_
    {
        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("authenticate", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                    os__.writeString(pw);
                    if(certificates == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(certificates.Length);
                        for(int ix__ = 0; ix__ < certificates.Length; ++ix__)
                        {
                            Murmur.CertificateDerHelper.write(os__, certificates[ix__]);
                        }
                    }
                    os__.writeString(certhash);
                    os__.writeBool(certstrong);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    newname = is__.readString();
                    groups = is__.readStringSeq();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getInfo", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    info = Murmur.UserInfoMapHelper.read(is__);
                    bool ret__;
                    ret__ = is__.readBool();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("idToName", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    string ret__;
                    ret__ = is__.readString();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("idToTexture", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    byte[] ret__;
                    ret__ = is__.readByteSeq();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("nameToId", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getRegisteredUsers", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(filter);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<int, string> ret__;
                    ret__ = Murmur.NameMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("registerUser", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.UserInfoMapHelper.write(os__, info);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setInfo", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                    Murmur.UserInfoMapHelper.write(os__, info);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setTexture", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                    os__.writeByteSeq(tex);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("unregisterUser", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class ServerDelM_ : Ice.ObjectDelM_, ServerDel_
    {
        public void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("addCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerCallbackPrxHelper.write__(os__, cb);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("addChannel", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                    os__.writeInt(parent);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("addContextCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                    os__.writeString(action);
                    os__.writeString(text);
                    Murmur.ServerContextCallbackPrxHelper.write__(os__, cb);
                    os__.writeInt(ctx);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("addUserToGroup", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                    os__.writeInt(session);
                    os__.writeString(group);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void delete(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("delete", Ice.OperationMode.Normal, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getACL", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 16);
                        acls = new Murmur.ACL[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            acls[ix__] = new Murmur.ACL();
                            acls[ix__].read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 7);
                        groups = new Murmur.Group[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            groups[ix__] = new Murmur.Group();
                            groups[ix__].read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    inherit = is__.readBool();
                    is__.endReadEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getAllConf", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<string, string> ret__;
                    ret__ = Murmur.ConfigMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getBans", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.Ban[] ret__;
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 20);
                        ret__ = new Murmur.Ban[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            ret__[ix__] = new Murmur.Ban();
                            ret__[ix__].read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getChannelState", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.Channel ret__;
                    ret__ = null;
                    if(ret__ == null)
                    {
                        ret__ = new Murmur.Channel();
                    }
                    ret__.read__(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getChannels", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<int, Murmur.Channel> ret__;
                    ret__ = Murmur.ChannelMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getConf", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(key);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    string ret__;
                    ret__ = is__.readString();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getLog", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(first);
                    os__.writeInt(last);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.LogEntry[] ret__;
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 5);
                        ret__ = new Murmur.LogEntry[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            ret__[ix__] = new Murmur.LogEntry();
                            ret__[ix__].read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getRegisteredUsers", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(filter);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<int, string> ret__;
                    ret__ = Murmur.NameMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getRegistration", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(userid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> ret__;
                    ret__ = Murmur.UserInfoMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getState", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.User ret__;
                    ret__ = null;
                    if(ret__ == null)
                    {
                        ret__ = new Murmur.User();
                    }
                    ret__.read__(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getTexture", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(userid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    byte[] ret__;
                    ret__ = is__.readByteSeq();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getTree", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.Tree ret__;
                    IceInternal.ParamPatcher<Murmur.Tree> ret___PP = new IceInternal.ParamPatcher<Murmur.Tree>("::Murmur::Tree");
                    is__.readObject(ret___PP);
                    is__.readPendingObjects();
                    is__.endReadEncaps();
                    try
                    {
                        ret__ = (Murmur.Tree)ret___PP.value;
                    }
                    catch(System.InvalidCastException)
                    {
                        ret__ = null;
                        IceInternal.Ex.throwUOE(ret___PP.type(), ret___PP.value.ice_id());
                    }
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getUserIds", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeStringSeq(names);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<string, int> ret__;
                    ret__ = Murmur.IdMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getUserNames", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeIntSeq(ids);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<int, string> ret__;
                    ret__ = Murmur.NameMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getUsers", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<int, Murmur.User> ret__;
                    ret__ = Murmur.UserMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("hasPermission", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                    os__.writeInt(channelid);
                    os__.writeInt(perm);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    bool ret__;
                    ret__ = is__.readBool();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int id(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("id", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("isRunning", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    bool ret__;
                    ret__ = is__.readBool();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("kickUser", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                    os__.writeString(reason);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("redirectWhisperGroup", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                    os__.writeString(source);
                    os__.writeString(target);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("registerUser", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.UserInfoMapHelper.write(os__, info);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("removeCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerCallbackPrxHelper.write__(os__, cb);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("removeChannel", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("removeContextCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerContextCallbackPrxHelper.write__(os__, cb);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("removeUserFromGroup", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                    os__.writeInt(session);
                    os__.writeString(group);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("sendMessage", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(session);
                    os__.writeString(text);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("sendMessageChannel", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                    os__.writeBool(tree);
                    os__.writeString(text);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setACL", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(channelid);
                    if(acls == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(acls.Length);
                        for(int ix__ = 0; ix__ < acls.Length; ++ix__)
                        {
                            (acls[ix__] == null ? new Murmur.ACL() : acls[ix__]).write__(os__);
                        }
                    }
                    if(groups == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(groups.Length);
                        for(int ix__ = 0; ix__ < groups.Length; ++ix__)
                        {
                            (groups[ix__] == null ? new Murmur.Group() : groups[ix__]).write__(os__);
                        }
                    }
                    os__.writeBool(inherit);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setAuthenticator", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerAuthenticatorPrxHelper.write__(os__, auth);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setBans", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(bans == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(bans.Length);
                        for(int ix__ = 0; ix__ < bans.Length; ++ix__)
                        {
                            (bans[ix__] == null ? new Murmur.Ban() : bans[ix__]).write__(os__);
                        }
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setChannelState", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setConf", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(key);
                    os__.writeString(value);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setState", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    if(state == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        state.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidChannelException)
                        {
                            throw;
                        }
                        catch(Murmur.InvalidSessionException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setSuperuserPassword", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(pw);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("setTexture", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(userid);
                    os__.writeByteSeq(tex);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidTextureException)
                        {
                            throw;
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void start(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("start", Ice.OperationMode.Normal, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerFailureException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void stop(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("stop", Ice.OperationMode.Normal, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("unregisterUser", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(userid);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("updateRegistration", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(userid);
                    Murmur.UserInfoMapHelper.write(os__, info);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidUserException)
                        {
                            throw;
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("verifyPassword", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeString(name);
                    os__.writeString(pw);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.ServerBootedException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    int ret__;
                    ret__ = is__.readInt();
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class MetaCallbackDelM_ : Ice.ObjectDelM_, MetaCallbackDel_
    {
        public void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("started", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerPrxHelper.write__(os__, srv);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("stopped", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.ServerPrxHelper.write__(os__, srv);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                if(!og__.istr().isEmpty())
                {
                    try
                    {
                        if(!ok__)
                        {
                            try
                            {
                                og__.throwUserException();
                            }
                            catch(Ice.UserException ex__)
                            {
                                throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                            }
                        }
                        og__.istr().skipEmptyEncaps();
                    }
                    catch(Ice.LocalException ex__)
                    {
                        throw new IceInternal.LocalExceptionWrapper(ex__, false);
                    }
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }

    public sealed class MetaDelM_ : Ice.ObjectDelM_, MetaDel_
    {
        public void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("addCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.MetaCallbackPrxHelper.write__(os__, cb);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getAllServers", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.ServerPrx[] ret__;
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 2);
                        ret__ = new Murmur.ServerPrx[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            ret__[ix__] = Murmur.ServerPrxHelper.read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getBootedServers", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.ServerPrx[] ret__;
                    {
                        int szx__ = is__.readSize();
                        is__.startSeq(szx__, 2);
                        ret__ = new Murmur.ServerPrx[szx__];
                        for(int ix__ = 0; ix__ < szx__; ++ix__)
                        {
                            ret__[ix__] = Murmur.ServerPrxHelper.read__(is__);
                            is__.checkSeq();
                            is__.endElement();
                        }
                        is__.endSeq(szx__);
                    }
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getDefaultConf", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    _System.Collections.Generic.Dictionary<string, string> ret__;
                    ret__ = Murmur.ConfigMapHelper.read(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getServer", Ice.OperationMode.Idempotent, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    os__.writeInt(id);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.ServerPrx ret__;
                    ret__ = Murmur.ServerPrxHelper.read__(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("getVersion", Ice.OperationMode.Idempotent, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    major = is__.readInt();
                    minor = is__.readInt();
                    patch = is__.readInt();
                    text = is__.readString();
                    is__.endReadEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("newServer", Ice.OperationMode.Normal, context__);
            try
            {
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    IceInternal.BasicStream is__ = og__.istr();
                    is__.startReadEncaps();
                    Murmur.ServerPrx ret__;
                    ret__ = Murmur.ServerPrxHelper.read__(is__);
                    is__.endReadEncaps();
                    return ret__;
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }

        public void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            IceInternal.Outgoing og__ = handler__.getOutgoing("removeCallback", Ice.OperationMode.Normal, context__);
            try
            {
                try
                {
                    IceInternal.BasicStream os__ = og__.ostr();
                    Murmur.MetaCallbackPrxHelper.write__(os__, cb);
                }
                catch(Ice.LocalException ex__)
                {
                    og__.abort(ex__);
                }
                bool ok__ = og__.invoke();
                try
                {
                    if(!ok__)
                    {
                        try
                        {
                            og__.throwUserException();
                        }
                        catch(Murmur.InvalidCallbackException)
                        {
                            throw;
                        }
                        catch(Ice.UserException ex__)
                        {
                            throw new Ice.UnknownUserException(ex__.ice_name(), ex__);
                        }
                    }
                    og__.istr().skipEmptyEncaps();
                }
                catch(Ice.LocalException ex__)
                {
                    throw new IceInternal.LocalExceptionWrapper(ex__, false);
                }
            }
            finally
            {
                handler__.reclaimOutgoing(og__);
            }
        }
    }
}

namespace Murmur
{
    public sealed class TreeDelD_ : Ice.ObjectDelD_, TreeDel_
    {
    }

    public sealed class ServerCallbackDelD_ : Ice.ObjectDelD_, ServerCallbackDel_
    {
        public void channelCreated(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "channelCreated", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.channelCreated(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void channelRemoved(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "channelRemoved", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.channelRemoved(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void channelStateChanged(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "channelStateChanged", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.channelStateChanged(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void userConnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "userConnected", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.userConnected(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void userDisconnected(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "userDisconnected", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.userDisconnected(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void userStateChanged(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "userStateChanged", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerCallback servant__ = null;
                try
                {
                    servant__ = (ServerCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.userStateChanged(state, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }
    }

    public sealed class ServerContextCallbackDelD_ : Ice.ObjectDelD_, ServerContextCallbackDel_
    {
        public void contextAction(string action, Murmur.User usr, int session, int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "contextAction", Ice.OperationMode.Idempotent, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerContextCallback servant__ = null;
                try
                {
                    servant__ = (ServerContextCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.contextAction(action, usr, session, channelid, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }
    }

    public sealed class ServerAuthenticatorDelD_ : Ice.ObjectDelD_, ServerAuthenticatorDel_
    {
        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "authenticate", Ice.OperationMode.Idempotent, context__);
            string newnameHolder__ = null;
            string[] groupsHolder__ = null;
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.authenticate(name, pw, certificates, certhash, certstrong, out newnameHolder__, out groupsHolder__, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            newname = newnameHolder__;
            groups = groupsHolder__;
            return result__;
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "getInfo", Ice.OperationMode.Idempotent, context__);
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> infoHolder__ = null;
            bool result__ = false;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.getInfo(id, out infoHolder__, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            info = infoHolder__;
            return result__;
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "idToName", Ice.OperationMode.Idempotent, context__);
            string result__ = null;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.idToName(id, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "idToTexture", Ice.OperationMode.Idempotent, context__);
            byte[] result__ = null;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.idToTexture(id, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "nameToId", Ice.OperationMode.Idempotent, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.nameToId(name, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }
    }

    public sealed class ServerUpdatingAuthenticatorDelD_ : Ice.ObjectDelD_, ServerUpdatingAuthenticatorDel_
    {
        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "authenticate", Ice.OperationMode.Idempotent, context__);
            string newnameHolder__ = null;
            string[] groupsHolder__ = null;
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.authenticate(name, pw, certificates, certhash, certstrong, out newnameHolder__, out groupsHolder__, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            newname = newnameHolder__;
            groups = groupsHolder__;
            return result__;
        }

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "getInfo", Ice.OperationMode.Idempotent, context__);
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> infoHolder__ = null;
            bool result__ = false;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.getInfo(id, out infoHolder__, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            info = infoHolder__;
            return result__;
        }

        public string idToName(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "idToName", Ice.OperationMode.Idempotent, context__);
            string result__ = null;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.idToName(id, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public byte[] idToTexture(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "idToTexture", Ice.OperationMode.Idempotent, context__);
            byte[] result__ = null;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.idToTexture(id, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int nameToId(string name, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "nameToId", Ice.OperationMode.Idempotent, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.nameToId(name, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "getRegisteredUsers", Ice.OperationMode.Idempotent, context__);
            _System.Collections.Generic.Dictionary<int, string> result__ = null;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.getRegisteredUsers(filter, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "registerUser", Ice.OperationMode.Normal, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.registerUser(info, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "setInfo", Ice.OperationMode.Idempotent, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.setInfo(id, info, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int setTexture(int id, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "setTexture", Ice.OperationMode.Idempotent, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.setTexture(id, tex, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }

        public int unregisterUser(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "unregisterUser", Ice.OperationMode.Normal, context__);
            int result__ = 0;
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                ServerUpdatingAuthenticator servant__ = null;
                try
                {
                    servant__ = (ServerUpdatingAuthenticator)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                result__ = servant__.unregisterUser(id, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
            return result__;
        }
    }

    public sealed class ServerDelD_ : Ice.ObjectDelD_, ServerDel_
    {
        public void addCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public int addChannel(string name, int parent, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void addContextCallback(int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void addUserToGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void delete(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void getACL(int channelid, out Murmur.ACL[] acls, out Murmur.Group[] groups, out bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<string, string> getAllConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.Ban[] getBans(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.Channel getChannelState(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.Channel> getChannels(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public string getConf(string key, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.LogEntry[] getLog(int first, int last, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> getRegistration(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.User getState(int session, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public byte[] getTexture(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.Tree getTree(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<string, int> getUserIds(string[] names, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<int, string> getUserNames(int[] ids, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<int, Murmur.User> getUsers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public bool hasPermission(int session, int channelid, int perm, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public int id(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public bool isRunning(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void kickUser(int session, string reason, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void redirectWhisperGroup(int session, string source, string target, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void removeCallback(Murmur.ServerCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void removeChannel(int channelid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void removeContextCallback(Murmur.ServerContextCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void removeUserFromGroup(int channelid, int session, string group, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void sendMessage(int session, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void sendMessageChannel(int channelid, bool tree, string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setACL(int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setAuthenticator(Murmur.ServerAuthenticatorPrx auth, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setBans(Murmur.Ban[] bans, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setChannelState(Murmur.Channel state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setConf(string key, string value, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setState(Murmur.User state, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setSuperuserPassword(string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void setTexture(int userid, byte[] tex, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void start(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void stop(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void unregisterUser(int userid, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void updateRegistration(int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public int verifyPassword(string name, string pw, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }
    }

    public sealed class MetaCallbackDelD_ : Ice.ObjectDelD_, MetaCallbackDel_
    {
        public void started(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "started", Ice.OperationMode.Normal, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                MetaCallback servant__ = null;
                try
                {
                    servant__ = (MetaCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.started(srv, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }

        public void stopped(Murmur.ServerPrx srv, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            Ice.Current current__ = new Ice.Current();
            initCurrent__(ref current__, "stopped", Ice.OperationMode.Normal, context__);
            IceInternal.Direct.RunDelegate run__ = delegate(Ice.Object obj__)
            {
                MetaCallback servant__ = null;
                try
                {
                    servant__ = (MetaCallback)obj__;
                }
                catch(_System.InvalidCastException)
                {
                    throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
                }
                servant__.stopped(srv, current__);
                return Ice.DispatchStatus.DispatchOK;
            };
            IceInternal.Direct direct__ = null;
            try
            {
                direct__ = new IceInternal.Direct(current__, run__);
                try
                {
                    Ice.DispatchStatus status__ = direct__.servant().collocDispatch__(direct__);
                    _System.Diagnostics.Debug.Assert(status__ == Ice.DispatchStatus.DispatchOK);
                }
                finally
                {
                    direct__.destroy();
                }
            }
            catch(Ice.SystemException)
            {
                throw;
            }
            catch(System.Exception ex__)
            {
                IceInternal.LocalExceptionWrapper.throwWrapper(ex__);
            }
        }
    }

    public sealed class MetaDelD_ : Ice.ObjectDelD_, MetaDel_
    {
        public void addCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.ServerPrx[] getAllServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.ServerPrx[] getBootedServers(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public _System.Collections.Generic.Dictionary<string, string> getDefaultConf(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.ServerPrx getServer(int id, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void getVersion(out int major, out int minor, out int patch, out string text, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public Murmur.ServerPrx newServer(_System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }

        public void removeCallback(Murmur.MetaCallbackPrx cb, _System.Collections.Generic.Dictionary<string, string> context__)
        {
            throw new Ice.CollocationOptimizationException();
        }
    }
}

namespace Murmur
{
    public abstract class ServerCallbackDisp_ : Ice.ObjectImpl, ServerCallback
    {
        #region Slice operations

        public void userConnected(Murmur.User state)
        {
            userConnected(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void userConnected(Murmur.User state, Ice.Current current__);

        public void userDisconnected(Murmur.User state)
        {
            userDisconnected(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void userDisconnected(Murmur.User state, Ice.Current current__);

        public void userStateChanged(Murmur.User state)
        {
            userStateChanged(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void userStateChanged(Murmur.User state, Ice.Current current__);

        public void channelCreated(Murmur.Channel state)
        {
            channelCreated(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void channelCreated(Murmur.Channel state, Ice.Current current__);

        public void channelRemoved(Murmur.Channel state)
        {
            channelRemoved(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void channelRemoved(Murmur.Channel state, Ice.Current current__);

        public void channelStateChanged(Murmur.Channel state)
        {
            channelStateChanged(state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void channelStateChanged(Murmur.Channel state, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::ServerCallback"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus userConnected___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.User state;
            state = null;
            if(state == null)
            {
                state = new Murmur.User();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.userConnected(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus userDisconnected___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.User state;
            state = null;
            if(state == null)
            {
                state = new Murmur.User();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.userDisconnected(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus userStateChanged___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.User state;
            state = null;
            if(state == null)
            {
                state = new Murmur.User();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.userStateChanged(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus channelCreated___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.Channel state;
            state = null;
            if(state == null)
            {
                state = new Murmur.Channel();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.channelCreated(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus channelRemoved___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.Channel state;
            state = null;
            if(state == null)
            {
                state = new Murmur.Channel();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.channelRemoved(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus channelStateChanged___(ServerCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.Channel state;
            state = null;
            if(state == null)
            {
                state = new Murmur.Channel();
            }
            state.read__(is__);
            is__.endReadEncaps();
            obj__.channelStateChanged(state, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        private static string[] all__ =
        {
            "channelCreated",
            "channelRemoved",
            "channelStateChanged",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "userConnected",
            "userDisconnected",
            "userStateChanged"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return channelCreated___(this, inS__, current__);
                }
                case 1:
                {
                    return channelRemoved___(this, inS__, current__);
                }
                case 2:
                {
                    return channelStateChanged___(this, inS__, current__);
                }
                case 3:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 4:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 5:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 6:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 7:
                {
                    return userConnected___(this, inS__, current__);
                }
                case 8:
                {
                    return userDisconnected___(this, inS__, current__);
                }
                case 9:
                {
                    return userStateChanged___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerCallback was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerCallback was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class ServerContextCallbackDisp_ : Ice.ObjectImpl, ServerContextCallback
    {
        #region Slice operations

        public void contextAction(string action, Murmur.User usr, int session, int channelid)
        {
            contextAction(action, usr, session, channelid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void contextAction(string action, Murmur.User usr, int session, int channelid, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::ServerContextCallback"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus contextAction___(ServerContextCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string action;
            action = is__.readString();
            Murmur.User usr;
            usr = null;
            if(usr == null)
            {
                usr = new Murmur.User();
            }
            usr.read__(is__);
            int session;
            session = is__.readInt();
            int channelid;
            channelid = is__.readInt();
            is__.endReadEncaps();
            obj__.contextAction(action, usr, session, channelid, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        private static string[] all__ =
        {
            "contextAction",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return contextAction___(this, inS__, current__);
                }
                case 1:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 2:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 3:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 4:
                {
                    return ice_ping___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerContextCallback was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerContextCallback was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class ServerAuthenticatorDisp_ : Ice.ObjectImpl, ServerAuthenticator
    {
        #region Slice operations

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, Ice.Current current__);

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return getInfo(id, out info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public int nameToId(string name)
        {
            return nameToId(name, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int nameToId(string name, Ice.Current current__);

        public string idToName(int id)
        {
            return idToName(id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract string idToName(int id, Ice.Current current__);

        public byte[] idToTexture(int id)
        {
            return idToTexture(id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract byte[] idToTexture(int id, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::ServerAuthenticator"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus authenticate___(ServerAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string name;
            name = is__.readString();
            string pw;
            pw = is__.readString();
            byte[][] certificates;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 1);
                certificates = new byte[szx__][];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    certificates[ix__] = Murmur.CertificateDerHelper.read(is__);
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            string certhash;
            certhash = is__.readString();
            bool certstrong;
            certstrong = is__.readBool();
            is__.endReadEncaps();
            string newname;
            string[] groups;
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, current__);
            os__.writeString(newname);
            os__.writeStringSeq(groups);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus getInfo___(ServerAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            is__.endReadEncaps();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info;
            IceInternal.BasicStream os__ = inS__.ostr();
            bool ret__ = obj__.getInfo(id, out info, current__);
            Murmur.UserInfoMapHelper.write(os__, info);
            os__.writeBool(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus nameToId___(ServerAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string name;
            name = is__.readString();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.nameToId(name, current__);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus idToName___(ServerAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            string ret__ = obj__.idToName(id, current__);
            os__.writeString(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus idToTexture___(ServerAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            byte[] ret__ = obj__.idToTexture(id, current__);
            os__.writeByteSeq(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        private static string[] all__ =
        {
            "authenticate",
            "getInfo",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "idToName",
            "idToTexture",
            "nameToId"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return authenticate___(this, inS__, current__);
                }
                case 1:
                {
                    return getInfo___(this, inS__, current__);
                }
                case 2:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 3:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 4:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 5:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 6:
                {
                    return idToName___(this, inS__, current__);
                }
                case 7:
                {
                    return idToTexture___(this, inS__, current__);
                }
                case 8:
                {
                    return nameToId___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerAuthenticator was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerAuthenticator was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class ServerUpdatingAuthenticatorDisp_ : Ice.ObjectImpl, ServerUpdatingAuthenticator
    {
        #region Slice operations

        public int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return registerUser(info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int registerUser(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public int unregisterUser(int id)
        {
            return unregisterUser(id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int unregisterUser(int id, Ice.Current current__);

        public _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter)
        {
            return getRegisteredUsers(filter, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract _System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, Ice.Current current__);

        public int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return setInfo(id, info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int setInfo(int id, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public int setTexture(int id, byte[] tex)
        {
            return setTexture(id, tex, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int setTexture(int id, byte[] tex, Ice.Current current__);

        #endregion

        #region Inherited Slice operations

        public int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups)
        {
            return authenticate(name, pw, certificates, certhash, certstrong, out newname, out groups, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, Ice.Current current__);

        public bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            return getInfo(id, out info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract bool getInfo(int id, out _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public string idToName(int id)
        {
            return idToName(id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract string idToName(int id, Ice.Current current__);

        public byte[] idToTexture(int id)
        {
            return idToTexture(id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract byte[] idToTexture(int id, Ice.Current current__);

        public int nameToId(string name)
        {
            return nameToId(name, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract int nameToId(string name, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::ServerAuthenticator",
            "::Murmur::ServerUpdatingAuthenticator"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[2];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[2];
        }

        public static new string ice_staticId()
        {
            return ids__[2];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus registerUser___(ServerUpdatingAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info;
            info = Murmur.UserInfoMapHelper.read(is__);
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.registerUser(info, current__);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus unregisterUser___(ServerUpdatingAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.unregisterUser(id, current__);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus getRegisteredUsers___(ServerUpdatingAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string filter;
            filter = is__.readString();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            _System.Collections.Generic.Dictionary<int, string> ret__ = obj__.getRegisteredUsers(filter, current__);
            Murmur.NameMapHelper.write(os__, ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus setInfo___(ServerUpdatingAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info;
            info = Murmur.UserInfoMapHelper.read(is__);
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.setInfo(id, info, current__);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus setTexture___(ServerUpdatingAuthenticator obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            byte[] tex;
            tex = is__.readByteSeq();
            is__.endReadEncaps();
            IceInternal.BasicStream os__ = inS__.ostr();
            int ret__ = obj__.setTexture(id, tex, current__);
            os__.writeInt(ret__);
            return Ice.DispatchStatus.DispatchOK;
        }

        private static string[] all__ =
        {
            "authenticate",
            "getInfo",
            "getRegisteredUsers",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "idToName",
            "idToTexture",
            "nameToId",
            "registerUser",
            "setInfo",
            "setTexture",
            "unregisterUser"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return Murmur.ServerAuthenticatorDisp_.authenticate___(this, inS__, current__);
                }
                case 1:
                {
                    return Murmur.ServerAuthenticatorDisp_.getInfo___(this, inS__, current__);
                }
                case 2:
                {
                    return getRegisteredUsers___(this, inS__, current__);
                }
                case 3:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 4:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 5:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 6:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 7:
                {
                    return Murmur.ServerAuthenticatorDisp_.idToName___(this, inS__, current__);
                }
                case 8:
                {
                    return Murmur.ServerAuthenticatorDisp_.idToTexture___(this, inS__, current__);
                }
                case 9:
                {
                    return Murmur.ServerAuthenticatorDisp_.nameToId___(this, inS__, current__);
                }
                case 10:
                {
                    return registerUser___(this, inS__, current__);
                }
                case 11:
                {
                    return setInfo___(this, inS__, current__);
                }
                case 12:
                {
                    return setTexture___(this, inS__, current__);
                }
                case 13:
                {
                    return unregisterUser___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerUpdatingAuthenticator was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::ServerUpdatingAuthenticator was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class ServerDisp_ : Ice.ObjectImpl, Server
    {
        #region Slice operations

        public void isRunning_async(Murmur.AMD_Server_isRunning cb__)
        {
            isRunning_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void isRunning_async(Murmur.AMD_Server_isRunning cb__, Ice.Current current__);

        public void start_async(Murmur.AMD_Server_start cb__)
        {
            start_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void start_async(Murmur.AMD_Server_start cb__, Ice.Current current__);

        public void stop_async(Murmur.AMD_Server_stop cb__)
        {
            stop_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void stop_async(Murmur.AMD_Server_stop cb__, Ice.Current current__);

        public void delete_async(Murmur.AMD_Server_delete cb__)
        {
            delete_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void delete_async(Murmur.AMD_Server_delete cb__, Ice.Current current__);

        public void id_async(Murmur.AMD_Server_id cb__)
        {
            id_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void id_async(Murmur.AMD_Server_id cb__, Ice.Current current__);

        public void addCallback_async(Murmur.AMD_Server_addCallback cb__, Murmur.ServerCallbackPrx cb)
        {
            addCallback_async(cb__, cb, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void addCallback_async(Murmur.AMD_Server_addCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__);

        public void removeCallback_async(Murmur.AMD_Server_removeCallback cb__, Murmur.ServerCallbackPrx cb)
        {
            removeCallback_async(cb__, cb, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void removeCallback_async(Murmur.AMD_Server_removeCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__);

        public void setAuthenticator_async(Murmur.AMD_Server_setAuthenticator cb__, Murmur.ServerAuthenticatorPrx auth)
        {
            setAuthenticator_async(cb__, auth, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setAuthenticator_async(Murmur.AMD_Server_setAuthenticator cb__, Murmur.ServerAuthenticatorPrx auth, Ice.Current current__);

        public void getConf_async(Murmur.AMD_Server_getConf cb__, string key)
        {
            getConf_async(cb__, key, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getConf_async(Murmur.AMD_Server_getConf cb__, string key, Ice.Current current__);

        public void getAllConf_async(Murmur.AMD_Server_getAllConf cb__)
        {
            getAllConf_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getAllConf_async(Murmur.AMD_Server_getAllConf cb__, Ice.Current current__);

        public void setConf_async(Murmur.AMD_Server_setConf cb__, string key, string value)
        {
            setConf_async(cb__, key, value, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setConf_async(Murmur.AMD_Server_setConf cb__, string key, string value, Ice.Current current__);

        public void setSuperuserPassword_async(Murmur.AMD_Server_setSuperuserPassword cb__, string pw)
        {
            setSuperuserPassword_async(cb__, pw, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setSuperuserPassword_async(Murmur.AMD_Server_setSuperuserPassword cb__, string pw, Ice.Current current__);

        public void getLog_async(Murmur.AMD_Server_getLog cb__, int first, int last)
        {
            getLog_async(cb__, first, last, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getLog_async(Murmur.AMD_Server_getLog cb__, int first, int last, Ice.Current current__);

        public void getUsers_async(Murmur.AMD_Server_getUsers cb__)
        {
            getUsers_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getUsers_async(Murmur.AMD_Server_getUsers cb__, Ice.Current current__);

        public void getChannels_async(Murmur.AMD_Server_getChannels cb__)
        {
            getChannels_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getChannels_async(Murmur.AMD_Server_getChannels cb__, Ice.Current current__);

        public void getTree_async(Murmur.AMD_Server_getTree cb__)
        {
            getTree_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getTree_async(Murmur.AMD_Server_getTree cb__, Ice.Current current__);

        public void getBans_async(Murmur.AMD_Server_getBans cb__)
        {
            getBans_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getBans_async(Murmur.AMD_Server_getBans cb__, Ice.Current current__);

        public void setBans_async(Murmur.AMD_Server_setBans cb__, Murmur.Ban[] bans)
        {
            setBans_async(cb__, bans, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setBans_async(Murmur.AMD_Server_setBans cb__, Murmur.Ban[] bans, Ice.Current current__);

        public void kickUser_async(Murmur.AMD_Server_kickUser cb__, int session, string reason)
        {
            kickUser_async(cb__, session, reason, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void kickUser_async(Murmur.AMD_Server_kickUser cb__, int session, string reason, Ice.Current current__);

        public void getState_async(Murmur.AMD_Server_getState cb__, int session)
        {
            getState_async(cb__, session, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getState_async(Murmur.AMD_Server_getState cb__, int session, Ice.Current current__);

        public void setState_async(Murmur.AMD_Server_setState cb__, Murmur.User state)
        {
            setState_async(cb__, state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setState_async(Murmur.AMD_Server_setState cb__, Murmur.User state, Ice.Current current__);

        public void sendMessage_async(Murmur.AMD_Server_sendMessage cb__, int session, string text)
        {
            sendMessage_async(cb__, session, text, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void sendMessage_async(Murmur.AMD_Server_sendMessage cb__, int session, string text, Ice.Current current__);

        public void hasPermission_async(Murmur.AMD_Server_hasPermission cb__, int session, int channelid, int perm)
        {
            hasPermission_async(cb__, session, channelid, perm, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void hasPermission_async(Murmur.AMD_Server_hasPermission cb__, int session, int channelid, int perm, Ice.Current current__);

        public void addContextCallback_async(Murmur.AMD_Server_addContextCallback cb__, int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx)
        {
            addContextCallback_async(cb__, session, action, text, cb, ctx, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void addContextCallback_async(Murmur.AMD_Server_addContextCallback cb__, int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, Ice.Current current__);

        public void removeContextCallback_async(Murmur.AMD_Server_removeContextCallback cb__, Murmur.ServerContextCallbackPrx cb)
        {
            removeContextCallback_async(cb__, cb, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void removeContextCallback_async(Murmur.AMD_Server_removeContextCallback cb__, Murmur.ServerContextCallbackPrx cb, Ice.Current current__);

        public void getChannelState_async(Murmur.AMD_Server_getChannelState cb__, int channelid)
        {
            getChannelState_async(cb__, channelid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getChannelState_async(Murmur.AMD_Server_getChannelState cb__, int channelid, Ice.Current current__);

        public void setChannelState_async(Murmur.AMD_Server_setChannelState cb__, Murmur.Channel state)
        {
            setChannelState_async(cb__, state, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setChannelState_async(Murmur.AMD_Server_setChannelState cb__, Murmur.Channel state, Ice.Current current__);

        public void removeChannel_async(Murmur.AMD_Server_removeChannel cb__, int channelid)
        {
            removeChannel_async(cb__, channelid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void removeChannel_async(Murmur.AMD_Server_removeChannel cb__, int channelid, Ice.Current current__);

        public void addChannel_async(Murmur.AMD_Server_addChannel cb__, string name, int parent)
        {
            addChannel_async(cb__, name, parent, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void addChannel_async(Murmur.AMD_Server_addChannel cb__, string name, int parent, Ice.Current current__);

        public void sendMessageChannel_async(Murmur.AMD_Server_sendMessageChannel cb__, int channelid, bool tree, string text)
        {
            sendMessageChannel_async(cb__, channelid, tree, text, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void sendMessageChannel_async(Murmur.AMD_Server_sendMessageChannel cb__, int channelid, bool tree, string text, Ice.Current current__);

        public void getACL_async(Murmur.AMD_Server_getACL cb__, int channelid)
        {
            getACL_async(cb__, channelid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getACL_async(Murmur.AMD_Server_getACL cb__, int channelid, Ice.Current current__);

        public void setACL_async(Murmur.AMD_Server_setACL cb__, int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit)
        {
            setACL_async(cb__, channelid, acls, groups, inherit, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setACL_async(Murmur.AMD_Server_setACL cb__, int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, Ice.Current current__);

        public void addUserToGroup_async(Murmur.AMD_Server_addUserToGroup cb__, int channelid, int session, string group)
        {
            addUserToGroup_async(cb__, channelid, session, group, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void addUserToGroup_async(Murmur.AMD_Server_addUserToGroup cb__, int channelid, int session, string group, Ice.Current current__);

        public void removeUserFromGroup_async(Murmur.AMD_Server_removeUserFromGroup cb__, int channelid, int session, string group)
        {
            removeUserFromGroup_async(cb__, channelid, session, group, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void removeUserFromGroup_async(Murmur.AMD_Server_removeUserFromGroup cb__, int channelid, int session, string group, Ice.Current current__);

        public void redirectWhisperGroup_async(Murmur.AMD_Server_redirectWhisperGroup cb__, int session, string source, string target)
        {
            redirectWhisperGroup_async(cb__, session, source, target, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void redirectWhisperGroup_async(Murmur.AMD_Server_redirectWhisperGroup cb__, int session, string source, string target, Ice.Current current__);

        public void getUserNames_async(Murmur.AMD_Server_getUserNames cb__, int[] ids)
        {
            getUserNames_async(cb__, ids, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getUserNames_async(Murmur.AMD_Server_getUserNames cb__, int[] ids, Ice.Current current__);

        public void getUserIds_async(Murmur.AMD_Server_getUserIds cb__, string[] names)
        {
            getUserIds_async(cb__, names, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getUserIds_async(Murmur.AMD_Server_getUserIds cb__, string[] names, Ice.Current current__);

        public void registerUser_async(Murmur.AMD_Server_registerUser cb__, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            registerUser_async(cb__, info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void registerUser_async(Murmur.AMD_Server_registerUser cb__, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public void unregisterUser_async(Murmur.AMD_Server_unregisterUser cb__, int userid)
        {
            unregisterUser_async(cb__, userid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void unregisterUser_async(Murmur.AMD_Server_unregisterUser cb__, int userid, Ice.Current current__);

        public void updateRegistration_async(Murmur.AMD_Server_updateRegistration cb__, int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info)
        {
            updateRegistration_async(cb__, userid, info, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void updateRegistration_async(Murmur.AMD_Server_updateRegistration cb__, int userid, _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__);

        public void getRegistration_async(Murmur.AMD_Server_getRegistration cb__, int userid)
        {
            getRegistration_async(cb__, userid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getRegistration_async(Murmur.AMD_Server_getRegistration cb__, int userid, Ice.Current current__);

        public void getRegisteredUsers_async(Murmur.AMD_Server_getRegisteredUsers cb__, string filter)
        {
            getRegisteredUsers_async(cb__, filter, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getRegisteredUsers_async(Murmur.AMD_Server_getRegisteredUsers cb__, string filter, Ice.Current current__);

        public void verifyPassword_async(Murmur.AMD_Server_verifyPassword cb__, string name, string pw)
        {
            verifyPassword_async(cb__, name, pw, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void verifyPassword_async(Murmur.AMD_Server_verifyPassword cb__, string name, string pw, Ice.Current current__);

        public void getTexture_async(Murmur.AMD_Server_getTexture cb__, int userid)
        {
            getTexture_async(cb__, userid, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getTexture_async(Murmur.AMD_Server_getTexture cb__, int userid, Ice.Current current__);

        public void setTexture_async(Murmur.AMD_Server_setTexture cb__, int userid, byte[] tex)
        {
            setTexture_async(cb__, userid, tex, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void setTexture_async(Murmur.AMD_Server_setTexture cb__, int userid, byte[] tex, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::Server"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus isRunning___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_isRunning cb__ = new _AMD_Server_isRunning(inS__);
            try
            {
                obj__.isRunning_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus start___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_start cb__ = new _AMD_Server_start(inS__);
            try
            {
                obj__.start_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus stop___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_stop cb__ = new _AMD_Server_stop(inS__);
            try
            {
                obj__.stop_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus delete___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_delete cb__ = new _AMD_Server_delete(inS__);
            try
            {
                obj__.delete_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus id___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_id cb__ = new _AMD_Server_id(inS__);
            try
            {
                obj__.id_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus addCallback___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerCallbackPrx cb;
            cb = Murmur.ServerCallbackPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Server_addCallback cb__ = new _AMD_Server_addCallback(inS__);
            try
            {
                obj__.addCallback_async(cb__, cb, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus removeCallback___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerCallbackPrx cb;
            cb = Murmur.ServerCallbackPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Server_removeCallback cb__ = new _AMD_Server_removeCallback(inS__);
            try
            {
                obj__.removeCallback_async(cb__, cb, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setAuthenticator___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerAuthenticatorPrx auth;
            auth = Murmur.ServerAuthenticatorPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Server_setAuthenticator cb__ = new _AMD_Server_setAuthenticator(inS__);
            try
            {
                obj__.setAuthenticator_async(cb__, auth, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getConf___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string key;
            key = is__.readString();
            is__.endReadEncaps();
            AMD_Server_getConf cb__ = new _AMD_Server_getConf(inS__);
            try
            {
                obj__.getConf_async(cb__, key, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getAllConf___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_getAllConf cb__ = new _AMD_Server_getAllConf(inS__);
            try
            {
                obj__.getAllConf_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setConf___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string key;
            key = is__.readString();
            string value;
            value = is__.readString();
            is__.endReadEncaps();
            AMD_Server_setConf cb__ = new _AMD_Server_setConf(inS__);
            try
            {
                obj__.setConf_async(cb__, key, value, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setSuperuserPassword___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string pw;
            pw = is__.readString();
            is__.endReadEncaps();
            AMD_Server_setSuperuserPassword cb__ = new _AMD_Server_setSuperuserPassword(inS__);
            try
            {
                obj__.setSuperuserPassword_async(cb__, pw, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getLog___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int first;
            first = is__.readInt();
            int last;
            last = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getLog cb__ = new _AMD_Server_getLog(inS__);
            try
            {
                obj__.getLog_async(cb__, first, last, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getUsers___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_getUsers cb__ = new _AMD_Server_getUsers(inS__);
            try
            {
                obj__.getUsers_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getChannels___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_getChannels cb__ = new _AMD_Server_getChannels(inS__);
            try
            {
                obj__.getChannels_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getTree___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_getTree cb__ = new _AMD_Server_getTree(inS__);
            try
            {
                obj__.getTree_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getBans___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Server_getBans cb__ = new _AMD_Server_getBans(inS__);
            try
            {
                obj__.getBans_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setBans___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.Ban[] bans;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 20);
                bans = new Murmur.Ban[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    bans[ix__] = new Murmur.Ban();
                    bans[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            is__.endReadEncaps();
            AMD_Server_setBans cb__ = new _AMD_Server_setBans(inS__);
            try
            {
                obj__.setBans_async(cb__, bans, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus kickUser___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            string reason;
            reason = is__.readString();
            is__.endReadEncaps();
            AMD_Server_kickUser cb__ = new _AMD_Server_kickUser(inS__);
            try
            {
                obj__.kickUser_async(cb__, session, reason, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getState___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getState cb__ = new _AMD_Server_getState(inS__);
            try
            {
                obj__.getState_async(cb__, session, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setState___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.User state;
            state = null;
            if(state == null)
            {
                state = new Murmur.User();
            }
            state.read__(is__);
            is__.endReadEncaps();
            AMD_Server_setState cb__ = new _AMD_Server_setState(inS__);
            try
            {
                obj__.setState_async(cb__, state, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus sendMessage___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            string text;
            text = is__.readString();
            is__.endReadEncaps();
            AMD_Server_sendMessage cb__ = new _AMD_Server_sendMessage(inS__);
            try
            {
                obj__.sendMessage_async(cb__, session, text, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus hasPermission___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            int channelid;
            channelid = is__.readInt();
            int perm;
            perm = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_hasPermission cb__ = new _AMD_Server_hasPermission(inS__);
            try
            {
                obj__.hasPermission_async(cb__, session, channelid, perm, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus addContextCallback___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            string action;
            action = is__.readString();
            string text;
            text = is__.readString();
            Murmur.ServerContextCallbackPrx cb;
            cb = Murmur.ServerContextCallbackPrxHelper.read__(is__);
            int ctx;
            ctx = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_addContextCallback cb__ = new _AMD_Server_addContextCallback(inS__);
            try
            {
                obj__.addContextCallback_async(cb__, session, action, text, cb, ctx, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus removeContextCallback___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerContextCallbackPrx cb;
            cb = Murmur.ServerContextCallbackPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Server_removeContextCallback cb__ = new _AMD_Server_removeContextCallback(inS__);
            try
            {
                obj__.removeContextCallback_async(cb__, cb, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getChannelState___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getChannelState cb__ = new _AMD_Server_getChannelState(inS__);
            try
            {
                obj__.getChannelState_async(cb__, channelid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setChannelState___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.Channel state;
            state = null;
            if(state == null)
            {
                state = new Murmur.Channel();
            }
            state.read__(is__);
            is__.endReadEncaps();
            AMD_Server_setChannelState cb__ = new _AMD_Server_setChannelState(inS__);
            try
            {
                obj__.setChannelState_async(cb__, state, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus removeChannel___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_removeChannel cb__ = new _AMD_Server_removeChannel(inS__);
            try
            {
                obj__.removeChannel_async(cb__, channelid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus addChannel___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string name;
            name = is__.readString();
            int parent;
            parent = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_addChannel cb__ = new _AMD_Server_addChannel(inS__);
            try
            {
                obj__.addChannel_async(cb__, name, parent, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus sendMessageChannel___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            bool tree;
            tree = is__.readBool();
            string text;
            text = is__.readString();
            is__.endReadEncaps();
            AMD_Server_sendMessageChannel cb__ = new _AMD_Server_sendMessageChannel(inS__);
            try
            {
                obj__.sendMessageChannel_async(cb__, channelid, tree, text, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getACL___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getACL cb__ = new _AMD_Server_getACL(inS__);
            try
            {
                obj__.getACL_async(cb__, channelid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setACL___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            Murmur.ACL[] acls;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 16);
                acls = new Murmur.ACL[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    acls[ix__] = new Murmur.ACL();
                    acls[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            Murmur.Group[] groups;
            {
                int szx__ = is__.readSize();
                is__.startSeq(szx__, 7);
                groups = new Murmur.Group[szx__];
                for(int ix__ = 0; ix__ < szx__; ++ix__)
                {
                    groups[ix__] = new Murmur.Group();
                    groups[ix__].read__(is__);
                    is__.checkSeq();
                    is__.endElement();
                }
                is__.endSeq(szx__);
            }
            bool inherit;
            inherit = is__.readBool();
            is__.endReadEncaps();
            AMD_Server_setACL cb__ = new _AMD_Server_setACL(inS__);
            try
            {
                obj__.setACL_async(cb__, channelid, acls, groups, inherit, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus addUserToGroup___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            int session;
            session = is__.readInt();
            string group;
            group = is__.readString();
            is__.endReadEncaps();
            AMD_Server_addUserToGroup cb__ = new _AMD_Server_addUserToGroup(inS__);
            try
            {
                obj__.addUserToGroup_async(cb__, channelid, session, group, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus removeUserFromGroup___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int channelid;
            channelid = is__.readInt();
            int session;
            session = is__.readInt();
            string group;
            group = is__.readString();
            is__.endReadEncaps();
            AMD_Server_removeUserFromGroup cb__ = new _AMD_Server_removeUserFromGroup(inS__);
            try
            {
                obj__.removeUserFromGroup_async(cb__, channelid, session, group, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus redirectWhisperGroup___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int session;
            session = is__.readInt();
            string source;
            source = is__.readString();
            string target;
            target = is__.readString();
            is__.endReadEncaps();
            AMD_Server_redirectWhisperGroup cb__ = new _AMD_Server_redirectWhisperGroup(inS__);
            try
            {
                obj__.redirectWhisperGroup_async(cb__, session, source, target, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getUserNames___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int[] ids;
            ids = is__.readIntSeq();
            is__.endReadEncaps();
            AMD_Server_getUserNames cb__ = new _AMD_Server_getUserNames(inS__);
            try
            {
                obj__.getUserNames_async(cb__, ids, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getUserIds___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string[] names;
            names = is__.readStringSeq();
            is__.endReadEncaps();
            AMD_Server_getUserIds cb__ = new _AMD_Server_getUserIds(inS__);
            try
            {
                obj__.getUserIds_async(cb__, names, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus registerUser___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info;
            info = Murmur.UserInfoMapHelper.read(is__);
            is__.endReadEncaps();
            AMD_Server_registerUser cb__ = new _AMD_Server_registerUser(inS__);
            try
            {
                obj__.registerUser_async(cb__, info, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus unregisterUser___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int userid;
            userid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_unregisterUser cb__ = new _AMD_Server_unregisterUser(inS__);
            try
            {
                obj__.unregisterUser_async(cb__, userid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus updateRegistration___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int userid;
            userid = is__.readInt();
            _System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info;
            info = Murmur.UserInfoMapHelper.read(is__);
            is__.endReadEncaps();
            AMD_Server_updateRegistration cb__ = new _AMD_Server_updateRegistration(inS__);
            try
            {
                obj__.updateRegistration_async(cb__, userid, info, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getRegistration___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int userid;
            userid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getRegistration cb__ = new _AMD_Server_getRegistration(inS__);
            try
            {
                obj__.getRegistration_async(cb__, userid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getRegisteredUsers___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string filter;
            filter = is__.readString();
            is__.endReadEncaps();
            AMD_Server_getRegisteredUsers cb__ = new _AMD_Server_getRegisteredUsers(inS__);
            try
            {
                obj__.getRegisteredUsers_async(cb__, filter, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus verifyPassword___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            string name;
            name = is__.readString();
            string pw;
            pw = is__.readString();
            is__.endReadEncaps();
            AMD_Server_verifyPassword cb__ = new _AMD_Server_verifyPassword(inS__);
            try
            {
                obj__.verifyPassword_async(cb__, name, pw, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getTexture___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int userid;
            userid = is__.readInt();
            is__.endReadEncaps();
            AMD_Server_getTexture cb__ = new _AMD_Server_getTexture(inS__);
            try
            {
                obj__.getTexture_async(cb__, userid, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus setTexture___(Server obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int userid;
            userid = is__.readInt();
            byte[] tex;
            tex = is__.readByteSeq();
            is__.endReadEncaps();
            AMD_Server_setTexture cb__ = new _AMD_Server_setTexture(inS__);
            try
            {
                obj__.setTexture_async(cb__, userid, tex, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        private static string[] all__ =
        {
            "addCallback",
            "addChannel",
            "addContextCallback",
            "addUserToGroup",
            "delete",
            "getACL",
            "getAllConf",
            "getBans",
            "getChannelState",
            "getChannels",
            "getConf",
            "getLog",
            "getRegisteredUsers",
            "getRegistration",
            "getState",
            "getTexture",
            "getTree",
            "getUserIds",
            "getUserNames",
            "getUsers",
            "hasPermission",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "id",
            "isRunning",
            "kickUser",
            "redirectWhisperGroup",
            "registerUser",
            "removeCallback",
            "removeChannel",
            "removeContextCallback",
            "removeUserFromGroup",
            "sendMessage",
            "sendMessageChannel",
            "setACL",
            "setAuthenticator",
            "setBans",
            "setChannelState",
            "setConf",
            "setState",
            "setSuperuserPassword",
            "setTexture",
            "start",
            "stop",
            "unregisterUser",
            "updateRegistration",
            "verifyPassword"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return addCallback___(this, inS__, current__);
                }
                case 1:
                {
                    return addChannel___(this, inS__, current__);
                }
                case 2:
                {
                    return addContextCallback___(this, inS__, current__);
                }
                case 3:
                {
                    return addUserToGroup___(this, inS__, current__);
                }
                case 4:
                {
                    return delete___(this, inS__, current__);
                }
                case 5:
                {
                    return getACL___(this, inS__, current__);
                }
                case 6:
                {
                    return getAllConf___(this, inS__, current__);
                }
                case 7:
                {
                    return getBans___(this, inS__, current__);
                }
                case 8:
                {
                    return getChannelState___(this, inS__, current__);
                }
                case 9:
                {
                    return getChannels___(this, inS__, current__);
                }
                case 10:
                {
                    return getConf___(this, inS__, current__);
                }
                case 11:
                {
                    return getLog___(this, inS__, current__);
                }
                case 12:
                {
                    return getRegisteredUsers___(this, inS__, current__);
                }
                case 13:
                {
                    return getRegistration___(this, inS__, current__);
                }
                case 14:
                {
                    return getState___(this, inS__, current__);
                }
                case 15:
                {
                    return getTexture___(this, inS__, current__);
                }
                case 16:
                {
                    return getTree___(this, inS__, current__);
                }
                case 17:
                {
                    return getUserIds___(this, inS__, current__);
                }
                case 18:
                {
                    return getUserNames___(this, inS__, current__);
                }
                case 19:
                {
                    return getUsers___(this, inS__, current__);
                }
                case 20:
                {
                    return hasPermission___(this, inS__, current__);
                }
                case 21:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 22:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 23:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 24:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 25:
                {
                    return id___(this, inS__, current__);
                }
                case 26:
                {
                    return isRunning___(this, inS__, current__);
                }
                case 27:
                {
                    return kickUser___(this, inS__, current__);
                }
                case 28:
                {
                    return redirectWhisperGroup___(this, inS__, current__);
                }
                case 29:
                {
                    return registerUser___(this, inS__, current__);
                }
                case 30:
                {
                    return removeCallback___(this, inS__, current__);
                }
                case 31:
                {
                    return removeChannel___(this, inS__, current__);
                }
                case 32:
                {
                    return removeContextCallback___(this, inS__, current__);
                }
                case 33:
                {
                    return removeUserFromGroup___(this, inS__, current__);
                }
                case 34:
                {
                    return sendMessage___(this, inS__, current__);
                }
                case 35:
                {
                    return sendMessageChannel___(this, inS__, current__);
                }
                case 36:
                {
                    return setACL___(this, inS__, current__);
                }
                case 37:
                {
                    return setAuthenticator___(this, inS__, current__);
                }
                case 38:
                {
                    return setBans___(this, inS__, current__);
                }
                case 39:
                {
                    return setChannelState___(this, inS__, current__);
                }
                case 40:
                {
                    return setConf___(this, inS__, current__);
                }
                case 41:
                {
                    return setState___(this, inS__, current__);
                }
                case 42:
                {
                    return setSuperuserPassword___(this, inS__, current__);
                }
                case 43:
                {
                    return setTexture___(this, inS__, current__);
                }
                case 44:
                {
                    return start___(this, inS__, current__);
                }
                case 45:
                {
                    return stop___(this, inS__, current__);
                }
                case 46:
                {
                    return unregisterUser___(this, inS__, current__);
                }
                case 47:
                {
                    return updateRegistration___(this, inS__, current__);
                }
                case 48:
                {
                    return verifyPassword___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Server was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Server was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class MetaCallbackDisp_ : Ice.ObjectImpl, MetaCallback
    {
        #region Slice operations

        public void started(Murmur.ServerPrx srv)
        {
            started(srv, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void started(Murmur.ServerPrx srv, Ice.Current current__);

        public void stopped(Murmur.ServerPrx srv)
        {
            stopped(srv, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void stopped(Murmur.ServerPrx srv, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::MetaCallback"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus started___(MetaCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerPrx srv;
            srv = Murmur.ServerPrxHelper.read__(is__);
            is__.endReadEncaps();
            obj__.started(srv, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        public static Ice.DispatchStatus stopped___(MetaCallback obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.ServerPrx srv;
            srv = Murmur.ServerPrxHelper.read__(is__);
            is__.endReadEncaps();
            obj__.stopped(srv, current__);
            return Ice.DispatchStatus.DispatchOK;
        }

        private static string[] all__ =
        {
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "started",
            "stopped"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 1:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 2:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 3:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 4:
                {
                    return started___(this, inS__, current__);
                }
                case 5:
                {
                    return stopped___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::MetaCallback was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::MetaCallback was not generated with stream support";
            throw ex;
        }

        #endregion
    }

    public abstract class MetaDisp_ : Ice.ObjectImpl, Meta
    {
        #region Slice operations

        public void getServer_async(Murmur.AMD_Meta_getServer cb__, int id)
        {
            getServer_async(cb__, id, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getServer_async(Murmur.AMD_Meta_getServer cb__, int id, Ice.Current current__);

        public void newServer_async(Murmur.AMD_Meta_newServer cb__)
        {
            newServer_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void newServer_async(Murmur.AMD_Meta_newServer cb__, Ice.Current current__);

        public void getBootedServers_async(Murmur.AMD_Meta_getBootedServers cb__)
        {
            getBootedServers_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getBootedServers_async(Murmur.AMD_Meta_getBootedServers cb__, Ice.Current current__);

        public void getAllServers_async(Murmur.AMD_Meta_getAllServers cb__)
        {
            getAllServers_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getAllServers_async(Murmur.AMD_Meta_getAllServers cb__, Ice.Current current__);

        public void getDefaultConf_async(Murmur.AMD_Meta_getDefaultConf cb__)
        {
            getDefaultConf_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getDefaultConf_async(Murmur.AMD_Meta_getDefaultConf cb__, Ice.Current current__);

        public void getVersion_async(Murmur.AMD_Meta_getVersion cb__)
        {
            getVersion_async(cb__, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void getVersion_async(Murmur.AMD_Meta_getVersion cb__, Ice.Current current__);

        public void addCallback_async(Murmur.AMD_Meta_addCallback cb__, Murmur.MetaCallbackPrx cb)
        {
            addCallback_async(cb__, cb, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void addCallback_async(Murmur.AMD_Meta_addCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__);

        public void removeCallback_async(Murmur.AMD_Meta_removeCallback cb__, Murmur.MetaCallbackPrx cb)
        {
            removeCallback_async(cb__, cb, Ice.ObjectImpl.defaultCurrent);
        }

        public abstract void removeCallback_async(Murmur.AMD_Meta_removeCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__);

        #endregion

        #region Slice type-related members

        public static new string[] ids__ = 
        {
            "::Ice::Object",
            "::Murmur::Meta"
        };

        public override bool ice_isA(string s)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override bool ice_isA(string s, Ice.Current current__)
        {
            return _System.Array.BinarySearch(ids__, s, IceUtilInternal.StringUtil.OrdinalStringComparer) >= 0;
        }

        public override string[] ice_ids()
        {
            return ids__;
        }

        public override string[] ice_ids(Ice.Current current__)
        {
            return ids__;
        }

        public override string ice_id()
        {
            return ids__[1];
        }

        public override string ice_id(Ice.Current current__)
        {
            return ids__[1];
        }

        public static new string ice_staticId()
        {
            return ids__[1];
        }

        #endregion

        #region Operation dispatch

        public static Ice.DispatchStatus getServer___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            int id;
            id = is__.readInt();
            is__.endReadEncaps();
            AMD_Meta_getServer cb__ = new _AMD_Meta_getServer(inS__);
            try
            {
                obj__.getServer_async(cb__, id, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus newServer___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Meta_newServer cb__ = new _AMD_Meta_newServer(inS__);
            try
            {
                obj__.newServer_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getBootedServers___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Meta_getBootedServers cb__ = new _AMD_Meta_getBootedServers(inS__);
            try
            {
                obj__.getBootedServers_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getAllServers___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Meta_getAllServers cb__ = new _AMD_Meta_getAllServers(inS__);
            try
            {
                obj__.getAllServers_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getDefaultConf___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Meta_getDefaultConf cb__ = new _AMD_Meta_getDefaultConf(inS__);
            try
            {
                obj__.getDefaultConf_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus getVersion___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Idempotent, current__.mode);
            inS__.istr().skipEmptyEncaps();
            AMD_Meta_getVersion cb__ = new _AMD_Meta_getVersion(inS__);
            try
            {
                obj__.getVersion_async(cb__, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus addCallback___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.MetaCallbackPrx cb;
            cb = Murmur.MetaCallbackPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Meta_addCallback cb__ = new _AMD_Meta_addCallback(inS__);
            try
            {
                obj__.addCallback_async(cb__, cb, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        public static Ice.DispatchStatus removeCallback___(Meta obj__, IceInternal.Incoming inS__, Ice.Current current__)
        {
            checkMode__(Ice.OperationMode.Normal, current__.mode);
            IceInternal.BasicStream is__ = inS__.istr();
            is__.startReadEncaps();
            Murmur.MetaCallbackPrx cb;
            cb = Murmur.MetaCallbackPrxHelper.read__(is__);
            is__.endReadEncaps();
            AMD_Meta_removeCallback cb__ = new _AMD_Meta_removeCallback(inS__);
            try
            {
                obj__.removeCallback_async(cb__, cb, current__);
            }
            catch(_System.Exception ex__)
            {
                cb__.ice_exception(ex__);
            }
            return Ice.DispatchStatus.DispatchAsync;
        }

        private static string[] all__ =
        {
            "addCallback",
            "getAllServers",
            "getBootedServers",
            "getDefaultConf",
            "getServer",
            "getVersion",
            "ice_id",
            "ice_ids",
            "ice_isA",
            "ice_ping",
            "newServer",
            "removeCallback"
        };

        public override Ice.DispatchStatus dispatch__(IceInternal.Incoming inS__, Ice.Current current__)
        {
            int pos = _System.Array.BinarySearch(all__, current__.operation, IceUtilInternal.StringUtil.OrdinalStringComparer);
            if(pos < 0)
            {
                throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
            }

            switch(pos)
            {
                case 0:
                {
                    return addCallback___(this, inS__, current__);
                }
                case 1:
                {
                    return getAllServers___(this, inS__, current__);
                }
                case 2:
                {
                    return getBootedServers___(this, inS__, current__);
                }
                case 3:
                {
                    return getDefaultConf___(this, inS__, current__);
                }
                case 4:
                {
                    return getServer___(this, inS__, current__);
                }
                case 5:
                {
                    return getVersion___(this, inS__, current__);
                }
                case 6:
                {
                    return ice_id___(this, inS__, current__);
                }
                case 7:
                {
                    return ice_ids___(this, inS__, current__);
                }
                case 8:
                {
                    return ice_isA___(this, inS__, current__);
                }
                case 9:
                {
                    return ice_ping___(this, inS__, current__);
                }
                case 10:
                {
                    return newServer___(this, inS__, current__);
                }
                case 11:
                {
                    return removeCallback___(this, inS__, current__);
                }
            }

            _System.Diagnostics.Debug.Assert(false);
            throw new Ice.OperationNotExistException(current__.id, current__.facet, current__.operation);
        }

        #endregion

        #region Marshaling support

        public override void write__(IceInternal.BasicStream os__)
        {
            os__.writeTypeId(ice_staticId());
            os__.startWriteSlice();
            os__.endWriteSlice();
            base.write__(os__);
        }

        public override void read__(IceInternal.BasicStream is__, bool rid__)
        {
            if(rid__)
            {
                /* string myId = */ is__.readTypeId();
            }
            is__.startReadSlice();
            is__.endReadSlice();
            base.read__(is__, true);
        }

        public override void write__(Ice.OutputStream outS__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Meta was not generated with stream support";
            throw ex;
        }

        public override void read__(Ice.InputStream inS__, bool rid__)
        {
            Ice.MarshalException ex = new Ice.MarshalException();
            ex.reason = "type Murmur::Meta was not generated with stream support";
            throw ex;
        }

        #endregion
    }
}

namespace Murmur
{
    public interface AMD_Server_isRunning
    {
        void ice_response(bool ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_isRunning : IceInternal.IncomingAsync, AMD_Server_isRunning
    {
        public _AMD_Server_isRunning(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(bool ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeBool(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_start
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_start : IceInternal.IncomingAsync, AMD_Server_start
    {
        public _AMD_Server_start(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerFailureException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_stop
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_stop : IceInternal.IncomingAsync, AMD_Server_stop
    {
        public _AMD_Server_stop(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_delete
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_delete : IceInternal.IncomingAsync, AMD_Server_delete
    {
        public _AMD_Server_delete(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_id
    {
        void ice_response(int ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_id : IceInternal.IncomingAsync, AMD_Server_id
    {
        public _AMD_Server_id(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(int ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeInt(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_addCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_addCallback : IceInternal.IncomingAsync, AMD_Server_addCallback
    {
        public _AMD_Server_addCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_removeCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_removeCallback : IceInternal.IncomingAsync, AMD_Server_removeCallback
    {
        public _AMD_Server_removeCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setAuthenticator
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setAuthenticator : IceInternal.IncomingAsync, AMD_Server_setAuthenticator
    {
        public _AMD_Server_setAuthenticator(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getConf
    {
        void ice_response(string ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getConf : IceInternal.IncomingAsync, AMD_Server_getConf
    {
        public _AMD_Server_getConf(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(string ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeString(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_getAllConf
    {
        void ice_response(_System.Collections.Generic.Dictionary<string, string> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getAllConf : IceInternal.IncomingAsync, AMD_Server_getAllConf
    {
        public _AMD_Server_getAllConf(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<string, string> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.ConfigMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_setConf
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setConf : IceInternal.IncomingAsync, AMD_Server_setConf
    {
        public _AMD_Server_setConf(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_setSuperuserPassword
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setSuperuserPassword : IceInternal.IncomingAsync, AMD_Server_setSuperuserPassword
    {
        public _AMD_Server_setSuperuserPassword(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_getLog
    {
        void ice_response(Murmur.LogEntry[] ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getLog : IceInternal.IncomingAsync, AMD_Server_getLog
    {
        public _AMD_Server_getLog(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.LogEntry[] ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(ret__.Length);
                        for(int ix__ = 0; ix__ < ret__.Length; ++ix__)
                        {
                            (ret__[ix__] == null ? new Murmur.LogEntry() : ret__[ix__]).write__(os__);
                        }
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Server_getUsers
    {
        void ice_response(_System.Collections.Generic.Dictionary<int, Murmur.User> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getUsers : IceInternal.IncomingAsync, AMD_Server_getUsers
    {
        public _AMD_Server_getUsers(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<int, Murmur.User> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.UserMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getChannels
    {
        void ice_response(_System.Collections.Generic.Dictionary<int, Murmur.Channel> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getChannels : IceInternal.IncomingAsync, AMD_Server_getChannels
    {
        public _AMD_Server_getChannels(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<int, Murmur.Channel> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.ChannelMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getTree
    {
        void ice_response(Murmur.Tree ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getTree : IceInternal.IncomingAsync, AMD_Server_getTree
    {
        public _AMD_Server_getTree(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.Tree ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeObject(ret__);
                    os__.writePendingObjects();
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getBans
    {
        void ice_response(Murmur.Ban[] ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getBans : IceInternal.IncomingAsync, AMD_Server_getBans
    {
        public _AMD_Server_getBans(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.Ban[] ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(ret__.Length);
                        for(int ix__ = 0; ix__ < ret__.Length; ++ix__)
                        {
                            (ret__[ix__] == null ? new Murmur.Ban() : ret__[ix__]).write__(os__);
                        }
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setBans
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setBans : IceInternal.IncomingAsync, AMD_Server_setBans
    {
        public _AMD_Server_setBans(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_kickUser
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_kickUser : IceInternal.IncomingAsync, AMD_Server_kickUser
    {
        public _AMD_Server_kickUser(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getState
    {
        void ice_response(Murmur.User ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getState : IceInternal.IncomingAsync, AMD_Server_getState
    {
        public _AMD_Server_getState(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.User ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        Murmur.User tmp__ = new Murmur.User();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        ret__.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setState
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setState : IceInternal.IncomingAsync, AMD_Server_setState
    {
        public _AMD_Server_setState(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_sendMessage
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_sendMessage : IceInternal.IncomingAsync, AMD_Server_sendMessage
    {
        public _AMD_Server_sendMessage(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_hasPermission
    {
        void ice_response(bool ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_hasPermission : IceInternal.IncomingAsync, AMD_Server_hasPermission
    {
        public _AMD_Server_hasPermission(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(bool ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeBool(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_addContextCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_addContextCallback : IceInternal.IncomingAsync, AMD_Server_addContextCallback
    {
        public _AMD_Server_addContextCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_removeContextCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_removeContextCallback : IceInternal.IncomingAsync, AMD_Server_removeContextCallback
    {
        public _AMD_Server_removeContextCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getChannelState
    {
        void ice_response(Murmur.Channel ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getChannelState : IceInternal.IncomingAsync, AMD_Server_getChannelState
    {
        public _AMD_Server_getChannelState(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.Channel ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        Murmur.Channel tmp__ = new Murmur.Channel();
                        tmp__.write__(os__);
                    }
                    else
                    {
                        ret__.write__(os__);
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setChannelState
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setChannelState : IceInternal.IncomingAsync, AMD_Server_setChannelState
    {
        public _AMD_Server_setChannelState(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_removeChannel
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_removeChannel : IceInternal.IncomingAsync, AMD_Server_removeChannel
    {
        public _AMD_Server_removeChannel(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_addChannel
    {
        void ice_response(int ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_addChannel : IceInternal.IncomingAsync, AMD_Server_addChannel
    {
        public _AMD_Server_addChannel(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(int ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeInt(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_sendMessageChannel
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_sendMessageChannel : IceInternal.IncomingAsync, AMD_Server_sendMessageChannel
    {
        public _AMD_Server_sendMessageChannel(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getACL
    {
        void ice_response(Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getACL : IceInternal.IncomingAsync, AMD_Server_getACL
    {
        public _AMD_Server_getACL(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(acls == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(acls.Length);
                        for(int ix__ = 0; ix__ < acls.Length; ++ix__)
                        {
                            (acls[ix__] == null ? new Murmur.ACL() : acls[ix__]).write__(os__);
                        }
                    }
                    if(groups == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(groups.Length);
                        for(int ix__ = 0; ix__ < groups.Length; ++ix__)
                        {
                            (groups[ix__] == null ? new Murmur.Group() : groups[ix__]).write__(os__);
                        }
                    }
                    os__.writeBool(inherit);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setACL
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setACL : IceInternal.IncomingAsync, AMD_Server_setACL
    {
        public _AMD_Server_setACL(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_addUserToGroup
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_addUserToGroup : IceInternal.IncomingAsync, AMD_Server_addUserToGroup
    {
        public _AMD_Server_addUserToGroup(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_removeUserFromGroup
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_removeUserFromGroup : IceInternal.IncomingAsync, AMD_Server_removeUserFromGroup
    {
        public _AMD_Server_removeUserFromGroup(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidChannelException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_redirectWhisperGroup
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_redirectWhisperGroup : IceInternal.IncomingAsync, AMD_Server_redirectWhisperGroup
    {
        public _AMD_Server_redirectWhisperGroup(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidSessionException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getUserNames
    {
        void ice_response(_System.Collections.Generic.Dictionary<int, string> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getUserNames : IceInternal.IncomingAsync, AMD_Server_getUserNames
    {
        public _AMD_Server_getUserNames(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<int, string> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.NameMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getUserIds
    {
        void ice_response(_System.Collections.Generic.Dictionary<string, int> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getUserIds : IceInternal.IncomingAsync, AMD_Server_getUserIds
    {
        public _AMD_Server_getUserIds(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<string, int> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.IdMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_registerUser
    {
        void ice_response(int ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_registerUser : IceInternal.IncomingAsync, AMD_Server_registerUser
    {
        public _AMD_Server_registerUser(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(int ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeInt(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_unregisterUser
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_unregisterUser : IceInternal.IncomingAsync, AMD_Server_unregisterUser
    {
        public _AMD_Server_unregisterUser(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_updateRegistration
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_updateRegistration : IceInternal.IncomingAsync, AMD_Server_updateRegistration
    {
        public _AMD_Server_updateRegistration(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getRegistration
    {
        void ice_response(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getRegistration : IceInternal.IncomingAsync, AMD_Server_getRegistration
    {
        public _AMD_Server_getRegistration(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<Murmur.UserInfo, string> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.UserInfoMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getRegisteredUsers
    {
        void ice_response(_System.Collections.Generic.Dictionary<int, string> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getRegisteredUsers : IceInternal.IncomingAsync, AMD_Server_getRegisteredUsers
    {
        public _AMD_Server_getRegisteredUsers(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<int, string> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.NameMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_verifyPassword
    {
        void ice_response(int ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_verifyPassword : IceInternal.IncomingAsync, AMD_Server_verifyPassword
    {
        public _AMD_Server_verifyPassword(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(int ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeInt(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_getTexture
    {
        void ice_response(byte[] ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_getTexture : IceInternal.IncomingAsync, AMD_Server_getTexture
    {
        public _AMD_Server_getTexture(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(byte[] ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeByteSeq(ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Server_setTexture
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Server_setTexture : IceInternal.IncomingAsync, AMD_Server_setTexture
    {
        public _AMD_Server_setTexture(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidTextureException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.InvalidUserException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(Murmur.ServerBootedException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Meta_getServer
    {
        void ice_response(Murmur.ServerPrx ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_getServer : IceInternal.IncomingAsync, AMD_Meta_getServer
    {
        public _AMD_Meta_getServer(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.ServerPrx ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.ServerPrxHelper.write__(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_newServer
    {
        void ice_response(Murmur.ServerPrx ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_newServer : IceInternal.IncomingAsync, AMD_Meta_newServer
    {
        public _AMD_Meta_newServer(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.ServerPrx ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.ServerPrxHelper.write__(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_getBootedServers
    {
        void ice_response(Murmur.ServerPrx[] ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_getBootedServers : IceInternal.IncomingAsync, AMD_Meta_getBootedServers
    {
        public _AMD_Meta_getBootedServers(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.ServerPrx[] ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(ret__.Length);
                        for(int ix__ = 0; ix__ < ret__.Length; ++ix__)
                        {
                            Murmur.ServerPrxHelper.write__(os__, ret__[ix__]);
                        }
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_getAllServers
    {
        void ice_response(Murmur.ServerPrx[] ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_getAllServers : IceInternal.IncomingAsync, AMD_Meta_getAllServers
    {
        public _AMD_Meta_getAllServers(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(Murmur.ServerPrx[] ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    if(ret__ == null)
                    {
                        os__.writeSize(0);
                    }
                    else
                    {
                        os__.writeSize(ret__.Length);
                        for(int ix__ = 0; ix__ < ret__.Length; ++ix__)
                        {
                            Murmur.ServerPrxHelper.write__(os__, ret__[ix__]);
                        }
                    }
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_getDefaultConf
    {
        void ice_response(_System.Collections.Generic.Dictionary<string, string> ret__);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_getDefaultConf : IceInternal.IncomingAsync, AMD_Meta_getDefaultConf
    {
        public _AMD_Meta_getDefaultConf(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(_System.Collections.Generic.Dictionary<string, string> ret__)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    Murmur.ConfigMapHelper.write(os__, ret__);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_getVersion
    {
        void ice_response(int major, int minor, int patch, string text);

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_getVersion : IceInternal.IncomingAsync, AMD_Meta_getVersion
    {
        public _AMD_Meta_getVersion(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response(int major, int minor, int patch, string text)
        {
            if(validateResponse__(true))
            {
                try
                {
                    IceInternal.BasicStream os__ = this.os__();
                    os__.writeInt(major);
                    os__.writeInt(minor);
                    os__.writeInt(patch);
                    os__.writeString(text);
                }
                catch(Ice.LocalException ex__)
                {
                    ice_exception(ex__);
                }
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            if(validateException__(ex))
            {
                exception__(ex);
            }
        }
    }

    public interface AMD_Meta_addCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_addCallback : IceInternal.IncomingAsync, AMD_Meta_addCallback
    {
        public _AMD_Meta_addCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }

    public interface AMD_Meta_removeCallback
    {
        void ice_response();

        void ice_exception(_System.Exception ex);
    }

    class _AMD_Meta_removeCallback : IceInternal.IncomingAsync, AMD_Meta_removeCallback
    {
        public _AMD_Meta_removeCallback(IceInternal.Incoming inc) : base(inc)
        {
        }

        public void ice_response()
        {
            if(validateResponse__(true))
            {
                response__(true);
            }
        }

        public void ice_exception(_System.Exception ex)
        {
            try
            {
                throw ex;
            }
            catch(Murmur.InvalidCallbackException ex__)
            {
                if(validateResponse__(false))
                {
                    os__().writeUserException(ex__);
                    response__(false);
                }
            }
            catch(_System.Exception ex__)
            {
                exception__(ex__);
            }
        }
    }
}
