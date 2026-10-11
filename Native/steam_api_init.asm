; SteamAPI_Init was removed from current steam_api64.dll.
; Facepunch.Steamworks still calls it, so this export calls SteamAPI_InitFlat
; in steam_api64_real.dll, which sits next to this shim.

option casemap:none

EXTERN GetModuleHandleW:PROC
EXTERN GetModuleFileNameW:PROC
EXTERN LoadLibraryW:PROC
EXTERN GetProcAddress:PROC

.data
    moduleName dw "s","t","e","a","m","_","a","p","i","6","4",".","d","l","l",0
    realName   dw "s","t","e","a","m","_","a","p","i","6","4","_","r","e","a","l",".","d","l","l",0
    initName   db "SteamAPI_InitFlat",0
    initFlat   dq 0
    errBuf     db 1024 dup(0)
    pathBuf    dw 520 dup(0)

.code

PUBLIC SteamAPI_Init

SteamAPI_Init PROC
    sub rsp, 28h
    cmp qword ptr [initFlat], 0
    jne call_flat

    lea rcx, moduleName
    call GetModuleHandleW
    test rax, rax
    jz fail

    mov rcx, rax
    lea rdx, pathBuf
    mov r8d, 520
    call GetModuleFileNameW
    test eax, eax
    jz fail

    lea r10, pathBuf
    xor rcx, rcx
    xor rdx, rdx
find_slash:
    mov ax, word ptr [r10+rcx*2]
    test ax, ax
    jz have_slash
    cmp ax, 5Ch
    jne next_ch
    mov rdx, rcx
next_ch:
    inc rcx
    jmp find_slash
have_slash:
    inc rdx
    lea r10, [r10+rdx*2]
    lea r11, realName
    mov rcx, 21
copy_name:
    mov ax, word ptr [r11]
    mov word ptr [r10], ax
    add r11, 2
    add r10, 2
    dec rcx
    jnz copy_name

    lea rcx, pathBuf
    call LoadLibraryW
    test rax, rax
    jz fail

    mov rcx, rax
    lea rdx, initName
    call GetProcAddress
    test rax, rax
    jz fail
    mov initFlat, rax

call_flat:
    lea rcx, errBuf
    mov rax, initFlat
    call rax
    test eax, eax
    setz al
    movzx eax, al
    add rsp, 28h
    ret

fail:
    xor eax, eax
    add rsp, 28h
    ret
SteamAPI_Init ENDP

END
