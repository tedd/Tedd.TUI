using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class BashLanguage : ILanguage
{
    public string Id => "bash";
    public string[] Aliases => [ "sh", "shell"  ];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var envVars = @"\b(?:BASH|BASHOPTS|BASH_ALIASES|BASH_ARGC|BASH_ARGV|BASH_CMDS|BASH_COMPLETION_COMPAT_DIR|BASH_LINENO|BASH_REMATCH|BASH_SOURCE|BASH_VERSINFO|BASH_VERSION|COLORTERM|COLUMNS|COMP_WORDBREAKS|DBUS_SESSION_BUS_ADDRESS|DEFAULTS_PATH|DESKTOP_SESSION|DIRSTACK|DISPLAY|EUID|GDMSESSION|GDM_LANG|GNOME_KEYRING_CONTROL|GNOME_KEYRING_PID|GPG_AGENT_INFO|GROUPS|HISTCONTROL|HISTFILE|HISTFILESIZE|HISTSIZE|HOME|HOSTNAME|HOSTTYPE|IFS|INSTANCE|JOB|LANG|LANGUAGE|LC_ADDRESS|LC_ALL|LC_IDENTIFICATION|LC_MEASUREMENT|LC_MONETARY|LC_NAME|LC_NUMERIC|LC_PAPER|LC_TELEPHONE|LC_TIME|LESSCLOSE|LESSOPEN|LINES|LOGNAME|LS_COLORS|MACHTYPE|MAILCHECK|MANDATORY_PATH|NO_AT_BRIDGE|OLDPWD|OPTERR|OPTIND|ORBIT_SOCKETDIR|OSTYPE|PAPERSIZE|PATH|PIPESTATUS|PPID|PS1|PS2|PS3|PS4|PWD|RANDOM|REPLY|SECONDS|SELINUX_INIT|SESSION|SESSIONTYPE|SESSION_MANAGER|SHELL|SHELLOPTS|SHLVL|SSH_AUTH_SOCK|TERM|UID|UPSTART_EVENTS|UPSTART_INSTANCE|UPSTART_JOB|UPSTART_SESSION|USER|WINDOWID|XAUTHORITY|XDG_CONFIG_DIRS|XDG_CURRENT_DESKTOP|XDG_DATA_DIRS|XDG_GREETER_DATA_DIR|XDG_MENU_PREFIX|XDG_RUNTIME_DIR|XDG_SEAT|XDG_SEAT_PATH|XDG_SESSION_DESKTOP|XDG_SESSION_ID|XDG_SESSION_PATH|XDG_SESSION_TYPE|XDG_VTNR|XMODIFIERS)\b";

        var commandAfterHeredoc = new Pattern(@"(^([""']?)\w+\2)[ \t]+\S.*", lookbehind: true, alias: "punctuation");

        var insideString = new Grammar();
        insideString.Add("bash", commandAfterHeredoc);
        insideString.Add("environment", new Pattern(@"\$" + envVars, alias: "constant"));

        var variableInside = new Grammar();
        // Simplified variable inside
        variableInside.Add("operator", new Pattern(@"--|\+\+|\*\*=?|<<=?|>>=?|&&|\|\||[=!+\-*/%<>^&|]=?|[?~:]"));
        variableInside.Add("punctuation", new Pattern(@"\(\(?|\)\)?|,|;"));
        variableInside.Add("number", new Pattern(@"\b0x[\dA-Fa-f]+\b|(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:[Ee]-?\d+)?"));

        insideString.Add("variable", new List<Pattern>
        {
            new Pattern(@"\$?\(\([\s\S]+?\)\)", greedy: true, inside: variableInside), // Arithmetic
            new Pattern(@"\$\((?:\([^)]+\)|[^()])+\)|`[^`]+`", greedy: true, inside: new Grammar { { "variable", new List<Pattern> { new Pattern(@"^\$\(|^`|\)$|`$") } } }), // Command sub
            new Pattern(@"\$\{[^}]+\}", greedy: true, inside: new Grammar { { "operator", new List<Pattern> { new Pattern(@":[-=?+]?|[!\/]|##?|%%?|\^\^?|,,?") } }, { "punctuation", new List<Pattern> { new Pattern(@"[\[\]]") } }, { "environment", new List<Pattern> { new Pattern(@"(\{)" + envVars, lookbehind: true, alias: "constant") } } }), // Brace expansion
            new Pattern(@"\$(?:\w+|[#?*!@$])")
        });

        insideString.Add("entity", new Pattern(@"\\(?:[abceEfnrtv\\""]|O?[0-7]{1,3}|U[0-9a-fA-F]{8}|u[0-9a-fA-F]{4}|x[0-9a-fA-F]{1,2})"));

        grammar.Add("shebang", new Pattern(@"^#!\s*\/.*", alias: "important"));
        grammar.Add("comment", new Pattern(@"(^|[^""{\\$])#.*", lookbehind: true));

        grammar.Add("function-name", new List<Pattern>
        {
            new Pattern(@"(\bfunction\s+)[\w-]+(?=(?:\s*\(?:\s*\))?\s*\{)", lookbehind: true, alias: "function"),
            new Pattern(@"\b[\w-]+(?=\s*\(\s*\)\s*\{)", alias: "function")
        });

        grammar.Add("for-or-select", new Pattern(@"(\b(?:for|select)\s+)\w+(?=\s+in\s)", alias: "variable", lookbehind: true));

        grammar.Add("assign-left", new Pattern(@"(^|[\s;|&]|[<>]\()\w+(?:\.\w+)*(?=\+?=)", inside: new Grammar { { "environment", new List<Pattern> { new Pattern(@"(^|[\s;|&]|[<>]\\())" + envVars, lookbehind: true, alias: "constant") } } }, alias: "variable", lookbehind: true));

        grammar.Add("parameter", new Pattern(@"(^|\s)-{1,2}(?:\w+:[+-]?)?\w+(?:\.\w+)*(?=[=\s]|$)", alias: "variable", lookbehind: true));

        grammar.Add("string", new List<Pattern>
        {
            new Pattern(@"((?:^|[^<])<<-?\s*)(\w+)\s[\s\S]*?(?:\r?\n|\r)\2", lookbehind: true, greedy: true, inside: insideString),
            new Pattern(@"((?:^|[^<])<<-?\s*)([""'])(\w+)\2\s[\s\S]*?(?:\r?\n|\r)\3", lookbehind: true, greedy: true, inside: new Grammar { { "bash", new List<Pattern> { commandAfterHeredoc } } }),
            new Pattern(@"(^|[^\\](?:\\\\)*)""(?:\\[\s\S]|\$\([^)]+\)|\$(?!\()|`[^`]+`|[^""\\`$])*""", lookbehind: true, greedy: true, inside: insideString),
            new Pattern(@"(^|[^$\\])'[^']*'", lookbehind: true, greedy: true),
            new Pattern(@"\$'(?:[^'\\]|\\[\s\S])*'", greedy: true, inside: new Grammar { { "entity", insideString["entity"] } })
        });

        grammar.Add("environment", new Pattern(@"\$?" + envVars, alias: "constant"));
        grammar.Add("variable", insideString["variable"]);

        grammar.Add("function", new Pattern(@"(^|[\s;|&]|[<>]\()(?:add|apropos|apt|apt-cache|apt-get|aptitude|aspell|automysqlbackup|awk|basename|bash|bc|bconsole|bg|bzip2|cal|cargo|cat|cfdisk|chgrp|chkconfig|chmod|chown|chroot|cksum|clear|cmp|column|comm|composer|cp|cron|crontab|csplit|curl|cut|date|dc|dd|ddrescue|debootstrap|df|diff|diff3|dig|dir|dircolors|dirname|dirs|dmesg|docker|docker-compose|du|egrep|eject|env|ethtool|expand|expect|expr|fdformat|fdisk|fg|fgrep|file|find|fmt|fold|format|free|fsck|ftp|fuser|gawk|git|gparted|grep|groupadd|groupdel|groupmod|groups|grub-mkconfig|gzip|halt|head|hg|history|host|hostname|htop|iconv|id|ifconfig|ifdown|ifup|import|install|ip|java|jobs|join|kill|killall|less|link|ln|locate|logname|logrotate|look|lpc|lpr|lprint|lprintd|lprintq|lprm|ls|lsof|lynx|make|man|mc|mdadm|mkconfig|mkdir|mke2fs|mkfifo|mkfs|mkisofs|mknod|mkswap|mmv|more|most|mount|mtools|mtr|mutt|mv|nano|nc|netstat|nice|nl|node|nohup|notify-send|npm|nslookup|op|open|parted|passwd|paste|pathchk|ping|pkill|pnpm|podman|podman-compose|popd|pr|printcap|printenv|ps|pushd|pv|quota|quotacheck|quotactl|ram|rar|rcp|reboot|remsync|rename|renice|rev|rm|rmdir|rpm|rsync|scp|screen|sdiff|sed|sendmail|seq|service|sftp|sh|shellcheck|shuf|shutdown|sleep|slocate|sort|split|ssh|stat|strace|su|sudo|sum|suspend|swapon|sync|sysctl|tac|tail|tar|tee|time|timeout|top|touch|tr|traceroute|tsort|tty|umount|uname|unexpand|uniq|units|unrar|unshar|unzip|update-grub|uptime|useradd|userdel|usermod|users|uudecode|uuencode|v|vcpkg|vdir|vi|vim|virsh|vmstat|wait|watch|wc|wget|whereis|which|who|whoami|write|xargs|xdg-open|yarn|yes|zenity|zip|zsh|zypper)(?=$|[)\s;|&])", lookbehind: true));

        grammar.Add("keyword", new Pattern(@"(^|[\s;|&]|[<>]\()(?:case|do|done|elif|else|esac|fi|for|function|if|in|select|then|until|while)(?=$|[)\s;|&])", lookbehind: true));
        grammar.Add("builtin", new Pattern(@"(^|[\s;|&]|[<>]\()(?:.|:|alias|bind|break|builtin|caller|cd|command|continue|declare|echo|enable|eval|exec|exit|export|getopts|hash|help|let|local|logout|mapfile|printf|pwd|read|readarray|readonly|return|set|shift|shopt|source|test|times|trap|type|typeset|ulimit|umask|unalias|unset)(?=$|[)\s;|&])", lookbehind: true, alias: "class-name"));
        grammar.Add("boolean", new Pattern(@"(^|[\s;|&]|[<>]\()(?:false|true)(?=$|[)\s;|&])", lookbehind: true));
        grammar.Add("file-descriptor", new Pattern(@"\B&\d\b", alias: "important"));

        var operatorInside = new Grammar();
        operatorInside.Add("file-descriptor", new Pattern(@"^\d", alias: "important"));
        grammar.Add("operator", new Pattern(@"\d?<>|>\||\+=|=[=~]?|!=?|<<[<-]?|[&\d]?>>|\d[<>]&?|[<>][&=]?|&[>&]?|\|[&|]?", inside: operatorInside));

        grammar.Add("punctuation", new Pattern(@"\$?\(\(?|\)\)?|\.\.|[{}[\];\\]"));
        grammar.Add("number", new Pattern(@"(^|\s)(?:[1-9]\d*|0)(?:[.,]\d+)?\b", lookbehind: true));

        // Note: commandAfterHeredoc.inside should be assigned 'grammar' (circular).
        commandAfterHeredoc.Inside = grammar;

        return grammar;
    }
}
