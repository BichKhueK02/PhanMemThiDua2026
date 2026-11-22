///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
namespace PhanMemThiDua2026
{
    // Logo của Form8 nằm thẳng trong mã nguồn (PNG 104x104, ~7,5 KB) thay vì trong tệp .resx.
    // Nhờ vậy Form8_CauHinhCSDL.resx giữ nguyên bản gốc của Visual Studio: không phải thay tệp .resx tải về
    // (MSBuild từ chối xử lý .resx có "mark of the web"), và ảnh GIF động 15 MB cũ không còn bị nạp vào RAM.
    public partial class Form8_CauHinhCSDL
    {
        private const string LogoPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAGgAAABoCAYAAAAdHLWhAAAdjklEQVR42u2de2xc95XfP2fubzgkh0NSfIqULIuWLVmyHpYVP+K3" +
            "48Tr1km22SDdbRpkG2y2WDTFosB226YomiLFFi226GJ30SZAm83u1k3jNKjrInUcG/EjtmVbsuzY0VvUi5Yoie83OZzfndM/fudq" +
            "rm1JpiRSorz8AcQM53Hvnd/5nXO+53vO+V1YGktjaSyNpbE0lsbSWBpLY2ksjaWxNBbRkI/dL1K9G1CgG5H+JQFdmUnPACuBB4Eu" +
            "4DngOuApRGY+8NkCUH1e4ag2AILIyLXw090iFko78IhMcycv9m7k1MkOHrn9eW3mKUS2X+CbEXAfqmsdOA8DwBlgBGi0559A9TjQ" +
            "BnhgOyIHlgQ0N8H8ugzyGV4e3ExjQ69uck9wd+e40FmjVQwC/R/QFo/ItP2/HvgUsAKIYsgAZROMCCyjmw4mp9vY/sJWmlr6Kfp6" +
            "8tUl3o736a2Z7wDPIVJcMnHvF0oW2CYvD/yxrmo5yfU8ARwFJggacK9pxijQDTwM/AJoMYHdBNwDDJpAaoAq4E2BL/HSyfXsfWcT" +
            "sz5PoTDBxq0HKRYd3XtvYrbo0GicjEB72zDljOgXNn4beAaRgSUNUm0G7pQ9fF3va/kPwD6gBNQBM0AnsN3+nwBWyT4eZj1rFGIT" +
            "1DIZYjVNZABHD5vYd3wV266/kTFKHDt6MzOlWpwrk4lmmBz3tLQP07G6zLFD63DkuGnDEYb7m5gpivz4l9/Gl/4I1R8p/NHV9Fdy" +
            "1Zy+SBnVVUAWyFMxR+P2vMoENQs0AZOAk1P8Hk89+7uUy6r/6NHfBA4BOaBfhviPURMHgUkftOgl4DH54RtfY3iwjqrcLKvXdpPJ" +
            "lKnJxaiD/XtuZHqilmUtA6xc1cuJYysZ7O9gyx07OXmsmUJDUR+76S+BP0Ok9PEXkGqTIbGi/eXssQTUpl6Lzawlmt4ub49/k5Mn" +
            "OnAR9PUvY/3m3Xp74Wk5wWZdyUtAu0Ctwpj0cTttdAOqUBLo4DCdDA4uo7F5miwxSpnunnaOHFgHWmLjtn20NU+yY/utjIw1sn7D" +
            "HoaHCvT113HT5mP6mfZ/isgrH18BqX4NWA/8V9MQgIIJZ9zilyJQb0LqBcYQmUT1G8AJ+cXpbzMzVQ8lIZsf4qGVu4CiwhAwLtCu" +
            "MCzQHAX/JDG0RjAUh2M2AdOMs4LXD36CM30t1OXPsHnrPlbTqxlOATk5wHpe3X4Pn717F9v33sjYaC3XrRjSh1Y9ici/+Xj5INUW" +
            "OcKfRrDDw+NAqwli1P6ypi2R+Zxh4Dgi45WlJP8Z1YLev/w+gSqmaGPqrOnzEqSbmKAJAxBxDM0KR3w45yhwwkGdL/CefGbtSf5v" +
            "5iHeO7SeG2aPaKbqoM1JlvHBFuKJJnbsXs+mjd1s37GZntMt8tby+xnSH+oy/hkiPde+BqkW5I2RZ6huPKNb+K5N/mjq3Flb1WUT" +
            "2mFEhi5wvI0O7gNaPBQF2k3bxh0cLgXfMwXUuSDwGmAM8B5igTsV9gnURzAbQzXj3MRLe2/j7g37aKLPri/HD3f8HUbGGtjyyZ2s" +
            "zo/ykxceJMrF3HbHIWrckK7jXyHy9rUrINVPST9fIUe/1vM98ytlwqQ22EQK8B4w8D6NuZDaq34rhusjOBrDTQpvCdwO9JjAqy0w" +
            "3QCcBlbaZ1bYOSMN5rMbuBGoFshoJZDtBaYEbuE7z/4BHV09NC+bpKaqzFuvb2btpt3U1E+xd/d1+rU7fxuRF689AQVO7B7gf5uf" +
            "abeYpmSaombSBoDxD1E2leN0APebKSsAAw42eTgItDuo9tDroMuH2KnRwMb3HfyODxqbAZaZALtMiycR+ZExFg1AQeALjLKFBkaA" +
            "UYXHpZc/ZM/RtazvOkpMmZ17bubMqeto7+wln4uBsv6tNd9A5IVrR0BhUr8E/Ao4afFM1sxNIxXTsxuREx9xrI0OHohhZQS9MdwQ" +
            "wbAPmlAEdiDyBqq/4eBGDweAW0ygsZ23LOH7E0AxhlYNn5t00O6D3+kGnrfr+7TAKvro1DbeAZBjfJoqhP09N3Fw31qWdx5n3aYD" +
            "7Nyxmbbr3tNHOxYE4S2UgO4z01JlK7fGJqxkJkaB0xf0NZVjfUHgYYVDEgBEXmG3wA0aznEakR+g+reBdkS+b8HvY0CLobqig/4Y" +
            "1gFDCmMShDSYMnt5YNoeZ+3aa+mhnVV0KxwxcOPl6Z7PsvedbazbsJfl7dPsfXMj19/UrQ+u+H1Enl+8Agqs80aDyQOpFZy31d5q" +
            "n8wBrwMPAK8jcuo8x6sBvgr0ObjOh4nrt+s+TtCCxFRm7Rw1wGfN1+1H5DVUbzcTu8wAyRkgZ+ax25jyaSDvoMoHH4aDXBwYjA6F" +
            "70iZf80JGlnFCYpEjFFNK8McoYGfPPsI27Ye0Xta/8l8mjuZZ+GsMZiaAZbb82mb1JxNZAmRdy/Sl60TuCsKznvcQ6/AFoUzDvJx" +
            "mHjRYE5LwNPArwN/fjb6V70LqBV4UAP70GaLaDVwykwdzgJlH86VNe3C/NoEcJ9Av4bJa6SXLgZnW9nzzlq8jvD37/iZCt+ck3W4" +
            "wgLqMrOQsAAF+3HLgWlG6aSBceAoIvsu4rhfBK53QQtLcXDo+QhOexiSsPqroiC4ISBG5Lvm/PsRKX/geGsNFADsMhS3CrhTLECO" +
            "YDiGpghO+QDXp+LAUAw4qIp/3vtbdHQe1Q28ChRlknsZI89TT3+ODbce4LrOPdrFb80HNSTzmLtZYyuuZKyymAZ506h6g74HPzRp" +
            "FU15631oTvWLhs6G7ZidLiyAWR/8T72DgoeZLJRKxtfZa1PGSPejWpOFL5fCtUwj8qNzsOltWXhEodmYh8EYOlJc4Jh9ugx08JN3" +
            "72Pt5kOsZW8EffE4N1Amw/9781HWrO3lzvpnVeRfLBYB3WrPqk1IDUbdRKZNfcAQImcuBgk6+LL5nT7TxMiueRSYddAaQ0MEMz6c" +
            "r2i+pS6GGg2TOgz8T+B37PqSBaQO2nyFoI3MjJ02IXTaeVqA8SyUS5CToG3TTHALO45vIJ+Pua2lm34aaOY4p6lhiBq2MqDwV4i8" +
            "dHUFFEyG2I8SE8gKm6xme/0ocOicmhOOUWfOvWxaNwN82sEGD30S0tsFYDwKEzwMLIvDYsgBmaiSCyIOYKRsr4/Z6s/GlRgpjgJQ" +
            "8DG0RzBhmde8QEcEJzwMmV86Yb9jwhbfCNDooCYusoUcMxzhZkan68nXTLCafp7bewcbN+zl+GiN3t/wGCKTV0dAwbTdbP+N2qTE" +
            "NgnTNum7ziuYcIx6gX8HjCj0G4rqMyceS5jUgq3wSQ0TGRkZOhGHc3gNQsvZd5bZtWQjmPJBqEOJxpmvnDTBxBKuN2fx1azB+Vjh" +
            "tMBy8z3VPpjQ7QTWoto0L01XrQR65AS30zPWSXt9UdfwCiL//FKnOHOZ+pMxkxBIzpPcbBdaBdQhsvOCwgmjTuEphaclTGCnCz80" +
            "a0GjS1a8objYhYkftdenFUou+JdJByWFsQiKJpyyC38ZH9DXCdOGUhwAQMaOVYyhxYFTOGX0T6vCqAks4wIMf9j+j1xFI7PAhAyT" +
            "B4Z0Jd2cOl1NPWPyf3Z/xfJeV83ErbJspzPIOm0rea9ROP1zPM46gd/TYHoGHbSZdqgGk3fCBJZz4VwJuZr3wUdMaIDHkSG+si2U" +
            "GaDGw6ir/N58HDSgEMGIDyZwxoLWQhS01MdBiycNMKw3WJ6z9IYnmN8NZnIlgun4tfF79ZOFJwVuZoYmDtNOF+9pXr5+tQS0LqWJ" +
            "y8/WEYgcvUiQ8RmbzJIx0eJhyoWLLJUqpqTahYmf9uF/sUVRtPezZmarHUQ+PMeB+vC5KUOWeRdWf9lXSNwpM1O7zKzWCqwxvzcU" +
            "Q5cJq860c8g4vOUasrd5oE6GeVCXcdBgez1PHrhTv7Du84gcvrImLmRHa+04deZA91+kcG538KhN8ETigM1vZDxMlMLxsy6s+jjF" +
            "HsyYaTNsQMYZULBUg3fB/GVT8VmdXW8pFYDmzVzlLGi9xdiGMQ2kbBloUtgfh8czCUCRSq3EMCKPI/JdXcb3BaoZYyUn6WBWAb5y" +
            "NXzQSjMjVTZhxbmmDFJjmQ+TMerCqi+aL5mxFT1rE102gcwSYPU0UEoeTfti0wZvDt37IMxx0zLsMWOx1AkfAMCInTNnxxwANrkQ" +
            "INd6eNcHuqle4V3jA3slJAP7zR/dZQsWRH6m0Ec9Y+SBDTcMy1/v/IeoNi58RjVUZhZSaA2LKyYukd5osZXfYtH7qMU+ST1CG1Bj" +
            "nFgn0BsF8jNjk+lTeaa8fW/WvltjxYtFm/y8CTMy8naCIIDIUJo6qDOflLAg1fa9AR+uoR3oF1ilcNAFv3vaB1ObXpx/ofCYNPJp" +
            "bazaLW/PrjcC938stAbdYXFFg/3ACJGey+CefmxmJ4rCD/RmtoI9hxaZ4G7KbDOQUE7llBKElrU/cWGyywYkxI6XjcN7JRcgeq0t" +
            "zhrzZz5Bh4TPOSDr4ZCHH7hKfHbSFkZRocdBh2n/jAua2phK0Q8B/yuCboEqvnrPj22xLaCJU62XF09+i1AoWGMrd89FfP9z53i1" +
            "LVV2NeuDWRIXKBvHCJ8wr3FM4ZXIsrDert18lfqgTeqDmUsep3w45ngU3seC1xnACbSaCSwLDPnwuhiF1OiCtjzk4RmBtUC9D6n1" +
            "1UnVkfm6Nh+A0fsRq8iMh12M0nkWnASGfuE0SG9ZcdRO5iwreTFFfTc41X+J6soUA77afmTJ+LPYmUYBGRo5ARyMQta0Opl4Y7VL" +
            "huaKKeid2O1cwqC7SvogNkqo7MIJYruuQikcS0ybMF+WgKBfs891mtXAQacPYGGND4nHc5sukaeZRDT4ucN2TQvmg5po5TWbiP6L" +
            "rWoxqPt4Kou6UYJdHjXTUhVDU1yByhmgz7QmTqGznL2XNUSXNyef+MQpV+EBHZCJQ5ySsXNURzAZQ60LH4qA1VEoePQOqgQKcRBu" +
            "k4fBCE7FwV92WYq9w/zV84j85II/vJN9JugeE/rIQgnoeoOTl9QJ4EX+7IMAQWHQB3+CC3TLjFUsxr4CkTOG1pK4bcaEkbGVPmMr" +
            "u2xMtjcyVU0zsbhKU5qEmB+zjGvWzOaYtwSgVMjVRh9yTQWLbSaNo9uX4hKnzseaKHRLL1/UTlYA+85W1s6riQtmaQx4cR5zsJMu" +
            "TJhz0GiJt6xF+pFU4p6yC6s9ssecoSsx+qXWheC2Jg5gocpV0h1iQCBj3xdLSUTWAZG1zGmtCyxFvQvCFUDjEKzWusAyjJn16LWk" +
            "YJ2D2x38geWUzje6OT6dSyUxmxfCB90KfP686elL4jHkDQ8vJbGMmTIiGIlgxtIIzpz+qK8EmJmU+Zo2565AlVbYiCR3owYUvCGz" +
            "BN3FEYxrxZeVTJOrPBQ9DCr0KYxGgaerJvB7pwUeEvgGsNrDYQ//HpFjF/iltTTWDDDM3cCxVMJwXgW00aLr+R0iO41nS66lZBNb" +
            "NtM0ZpA6CUwTkDCTmtTIBDCVFqAJfsaAR4ZgMssEAaSFWIrCZ2JjyrtMQ4sG0RPA0ZggwqRCCLjBkpHpbo0PjkjX85/k3f5bTXtm" +
            "FkJAJ6mgnvke75q5qU4l5XAVn5GYsGozX1VxMIm1SVzjAux3ElZnjvC5GqDJVUBFlHqvOg7HyxogSDoqxEBHlcH5ckK82nnqgLIx" +
            "4e2WBU7D63NNfg8wzQOtu4GtqezsvAqoGWhAtd3g8XyOKQ+vWhlVKUmT+6BFRR9MXckYBgVcBDMaNCYTVSgfF1U6JVwCqe07s75C" +
            "qnozcbGZ1FHg+sT8aUBpiUDHFIaikK+asusYtPNkPlQLlyTnklDCGA1CGuN3gRZEFkRAGYuU/xB42PL482XmBqye7CUf6P+yr7AF" +
            "6T88vJ78AS/7Sq1d2QLTSXttIglgzRROmEbM+AqfN5GYTYUDcpT7ziLOCszHivAjYyvytqBi4/vO95vSBZklsz43pCiseYTZqs1S" +
            "YXtF4RFrnDo2z/5ot0XatSmGOk7ldWo/VHOmWvYBld1pZjIfhTijxqVq5sy/JKjQp973BuWzcRfb42d7vuweWfVDM3WR+TgfhfxQ" +
            "vfF805bKmGt10ikrXlkN7F+IQLWQSlRNEXL+0YJ4owAazld3d/85Pv8qgFeNHdwbVbQtqW3IWzw1m9QjeBhxIeicNSHMuiCEKh5Z" +
            "9bgBkshMYwJOMknCz8xur8HtuZdDixxDNUK1DpGJ+TRx07bycuZUhVBGyxXbnEKkfN4uAtV7zRdEloVNUFddHK63BgMFlshrtHir" +
            "ypx/Sxwyo1EcPtdony9YXFRnBZKRvbcf+CUie+cYPzYDtai2EjKzm+dbg8YUJiRQG2UXiivqbFWcWnABXXhfBBw85FWLwKxWuvSc" +
            "hwmjl2JCXJXEPFOmDTOEosdZgu+bddCQMBsaYqwEqheN9jkEvHbBokTVWot1IrM+3vzPjNE8ZeYZJERmv705vJJFzvdYJ8NVHR5e" +
            "dXCXQeLYHHjGzFfZAtQEeCQseNk+k0leN180m/i+JO1gf5El8l76kHBCdVPyvNU0ZDOh+qc91TyQtfPPzLcG5Q0kxBJokSoJWP42" +
            "hTyqPZaw23lVJCTyvFdVB/fHgc9Tm4ycmSSigMTqo4qQ8oZCIldJo8/G0Cih4qdsxY2JL4p8EE6PCWKbdVM8DcxY7TdAPf000Uq/" +
            "LeSkkHPYAlp5X2A7TwKqs5U1YSRmzv4vO1jpwwmLqA5dSmHEPAnpBa8q1jRWTNgEDan0jLEPmqKLkjS5GGOR9YGEHTNSteQqVJI3" +
            "9n63CWezTfxzVIplkrq4flqZML89ZH8tppVHzjasqdbPJR6aq4AmTCA1MWSk0lIybRE1ccjX16G640q3qqc1iSCkrTaBCetddJXS" +
            "3nKKBsoZ9ZMxdoG4AsmTHNVsqr0FVD8P/JxQsPkwqmfsuEkKvc/cQQYYNI07juqGsz4o7BERzZ8PEjmjQXumLCI+aQ55LIJ+D/2p" +
            "nM2Gq+qQRH5ugWsSiBZTAkm0xrlgvryZrzjVy5RoWPKdJMW+HdW7GKXOfPFxe30ZMIvSRKgFdKYpVedAwkNADaoPnrfl8zKYhMQx" +
            "TloT7ljygxy0x2EVJfzUR/cSzT9dlAYN+wxt1ngDBFSeE8Mqb8FwijTFB/pnyleAwzRQkBf6vg58DsjSwC9ts6UCoX6u2ioMi6ZJ" +
            "YREEZqSM6krr/kuAQxcXYiAuQ0Cnkx8j0GokZTYOZq/RUM90DI2oflRady2wZSH9kZGaBakQrQ2G0mo17JWQmLxqKsRpFAeGIRJo" +
            "cwmz/Ym2ftsg8GVE9qL6JXmL3we2MGQlzoGRbwXeND8D0GuUz0GD1xPGKuxciIxqH6FWuWCO1EkwzkkMMWVk41spbTvfBO6/AsZu" +
            "yszwlIGAjNUnYMCg5Cr/x4auMkbpdFnNQ5VAEwUGzk56KHVWvY2ngdM0nS2ayZgQZ1HdbL5po/krb98/dbHVTxcjoHEqm0QknVnl" +
            "GHIa3psU6FT46VzTuQscG5WlxF3y5vS66JM1P/KVzgtSbTIkrLkLaC0mbI7Ra1vLlKz7Ow8MWpNZnlCHMITq5tSWZdcDL9vzLkLz" +
            "14wFrT2XOicXI6DDwN2W7HJmLspqaEhC3fK4v1SOLsQVGeuA2325DWUOauMdI2u4p/F1Q2slo39K9rutzTTU0sWhm0H46ZHfpG3F" +
            "gNuWezKu5Ja8maisxTC1qG4E7kAVE1qNCa3hA81smwmlaeMsaOmvyBkLVpusXDdKcv2EbraiDzuGDF5U0bxqlnH9e7bqyqlY4673" +
            "PV7cuMvDTHRP45NRaCUpGZsQx+/ya/ETe/6xVfpUmV+NLUNazXu9TdTkpnzoavBRQKrDqThoOyH9v8m2oak1Ae63Pt1bCPmlKRNq" +
            "k2kXC76ZUhQEUG11aNUWaWeN2S07uM7PRZVVHbDJhCvU8Z6Zz18ZwNiY5H/mTIuorkDkpP2oaoO5SUlwdFZj1jNA9paeeC+PsoFX" +
            "XIVZKAEF1t9yLNrAi0C9pRdKUWjfLKO6IQv3lML2mu0WG54gdABmU8nB0VSAfyjVKb6wxfM+VPcvJ7QGVtmmeTUu/JB2H2xuyxwO" +
            "1WWbELUDWyywfRUoGIQdO7tJkcgv53h5t6SuM5OiZ5I6bXWQibK8TQtjdJ6tdSgZwFGgHN237MfW3JUz4QxaEzPADaVwvJWEvX2e" +
            "tecJKdpkps2KlegjVANlr1SH3a+sZ6Yx2bLSUsBxqoPgxjnFVEGQR1NdB6Wzjlvk0CX8ll3nMAvOij66Ymi26p8yreynkf2RdTq4" +
            "wG7HBudWxtDFLJstCP8+oQk5yYvVC8Bp1tv/N5pfSnxM0bQ3b0Fssq3nFRCQyJBtiVIrYbW0GV81a3FQlJCQHzE6jCqpO+tzREqX" +
            "tTeoSNr3VVnTsLMc0Amje6otoB60rGsx1UsLE6xiltVAvVbxTQ//DZEhRBIT1eBgGZ7V7InrTFuGgCp5Yu/jRjE1meBmU6g3vlTW" +
            "/1I29PspoXge282wTUI9WuyTkuCPnszXFhhif8/BP4iCGZ5JOhKS+jkXWiVnDdAsS17TOl43czTwoc02VL9sTcgrcBT14ehbtsAL" +
            "cpivIKJG9STcXrUJJ/uBHUsWWEAiB1X1gMBWqeyUWBJ71JAneperO8Z9pdagbM9zqXq7IVvhGYPak9ZOOZ1s/neOY7bEle7v1w1+" +
            "TwF9uoY/Zs36SdOYOiqQPtnhK3OpArpUPmyPwl7rLpsxh3nM+jT3c7VHICJ/kHRIWCVOkoDzqeazenu/4Cq9rtGHBBQScgWO08Fx" +
            "M80VITTY91rMZCa7rJw2NOeBvZe6Lcyl7ln6qvFp01phe88AbyyiG1pkDPqnO/E0RUNlLBWhJo0xe6+esIvW+2JAVPs5MrhGH2p+" +
            "yhZha4qNqDICNNHI0gcYmDNXdlNZkaJlUTcY7V5rznGQxTMiSxloqhhyysxcravUd0+n4q0yIn96nuMdix5q/u8+0DnZVPN0ZPNY" +
            "ldozYch80ChJDHUVdv39hcCntCKY2MDDYrlJRc5bWbAV4U9aQUitQEMcYpwkRJgUWKPwwkew+S+YgL1pnKQ63CW1Z12LkaOzl7st" +
            "WeYy7PyEwlMCy609sIPFNELq/Vljs5NJLUilxi9vpcEqYdPZnnQsxbk72qvNBCaNxZaEDfsoAG/b42hqw3au3r7ZIi+raofAvVKk" +
            "XXO0LSINApGdTvXhpCMvDvVpRZvUTnnbb2Or263wM2OoL0RoDjPFcmbIMg2sOOuzysBJRA4aoEiKQiYuanevBdzY/BWFiBwrL6bv" +
            "5QqmHUZSvNigwEqUOg7RyFZ3QOF7wNtz8BOj1DJGLbWpTdgLwDsfqA0cAZbNh3DmYyMLEOlN7bDYOq9F9fOjRd81fzFuk5dBOM0K" +
            "RhT+BJFdc3Tiw0bZJFvGjCPyzIcKNwMjcpBFdWuAsP905GCtX7geossZdda7+pcq8ueoPkqe4xe5V5E3C9Fj1ToDcG3d3C+L6m/b" +
            "Bn+L/To38p5+FdXPphJsF/pOztLYN3LN3uBJpITqq4TcyMFFdi+8xrOd6SGi341q2Xi45ajqRxQRrrFmre5r//5BoZo/viJF9Zd5" +
            "0w9CFWpyS8/Dc7opFR+Pe5lmLWu6mK6p6pxCUq1ZdMDmCk1IfpFdT/MFTODSWJRafg2MzN9gEdVcksarrljIsuWrjaaWxjVwT+4P" +
            "73t6rTjpq3CdmStMu5TPA/VbrgkBXYX7qC7Wldq4NAmL4V7ewWRUp3YMnr5iZjbR5vB/LaGIpOHs/fQWSWAqf4Mgdbp+QOyxJZUe" +
            "TwpKCoRsaO9iMGmyqKmYJIEWJjg31905zqM1UWq/oVZG+SIN5OS5kw8zPNiof3fzE4Sb8HYvJkpHFvnKr0ttBNGQqmUbBz5PKPsq" +
            "2JYs5SysL4Uq0qQs6h3gAYGN/PWO36ChuZ+hwQ4m+jupqR1j7W3vcH/DLg3lvd1z7RtdEtD54XkjgVl+QP7LM/+W5tYh7t+2i1/1" +
            "NZLRaZob+6jPzTAw0s71jUN46tmxaxNjk53kCu+xaUuPbsy8TmghOXBRtzBYEtAl3XUlRyj76iK0syS3nB4DdhPqyJef3WdUxC/h" +
            "wqWxNJbG0lgaS2NpLI2lsTSWxtJYGkvjMsb/B56JQvNzQMsWAAAAAElFTkSuQmCC";
        private void NapLogo()
        {
            try
            {
                using var ms = new MemoryStream(Convert.FromBase64String(LogoPngBase64));
                using var goc = Image.FromStream(ms);
                picLogo.Image = new Bitmap(goc);   // bản sao độc lập, không phụ thuộc luồng nguồn
                Disposed += (_, _) =>
                {
                    picLogo.Image?.Dispose();
                    picLogo.Image = null;
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form8] Không nạp được logo: {ex.Message}");
            }
        }
    }
}
