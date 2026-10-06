namespace WebBanCameraGiamSat.Models
{
    public enum OrderStatus
    {
        ChoXacNhan = 0,
        DaXacNhan = 1,
        DangDongGoi = 2,
        DangVanChuyen = 3,
        DaGiaoHang = 4,
        DaHuy = 5
    }

    public enum PaymentMethod
    {
        COD = 0,
        VNPay = 1,
        MoMo = 2
    }

    public enum PaymentStatus
    {
        ChuaThanhToan = 0,
        DaThanhToan = 1,
        ThatBai = 2,
        DaHoanTien = 3
    }

    public enum DiscountType
    {
        Percent = 0,
        FixedAmount = 1
    }
}
