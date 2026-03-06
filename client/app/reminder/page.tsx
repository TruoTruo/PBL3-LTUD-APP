"use client"; // Bắt buộc phải có dòng này để dùng được useState và useEffect

import { useState } from "react";

export default function ReminderPage() {
  // State để lưu trữ dữ liệu form
  const [formData, setFormData] = useState({
    idAcc: 11, // Giả định ID tài khoản đang đăng nhập (lấy từ SQLQuery2)
    minsBefore: 15,
    isEnabled: true,
    channel: "App"
  });

  const [status, setStatus] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setStatus("Đang lưu...");

    try {
      const response = await fetch("http://localhost:5000/api/Reminder", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        setStatus("✅ Lưu cấu hình nhắc lịch thành công!");
      } else {
        const errorData = await response.json();
        setStatus(`❌ Lỗi: ${errorData.message || "Không thể lưu"}`);
      }
    } catch (error) {
      setStatus("❌ Lỗi kết nối đến Backend (Hãy chắc chắn API đang chạy)");
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h1 className="text-2xl font-bold mb-6 text-gray-800">Cài đặt nhắc lịch</h1>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">ID Tài khoản (Test):</label>
            <input 
              type="number"
              className="mt-1 block w-full border border-gray-300 rounded-md p-2"
              value={formData.idAcc}
              onChange={(e) => setFormData({...formData, idAcc: parseInt(e.target.value)})}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Thông báo trước (phút):</label>
            <select 
              className="mt-1 block w-full border border-gray-300 rounded-md p-2"
              value={formData.minsBefore}
              onChange={(e) => setFormData({...formData, minsBefore: parseInt(e.target.value)})}
            >
              <option value={15}>15 phút</option>
              <option value={30}>30 phút</option>
              <option value={60}>1 tiếng</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Kênh nhận:</label>
            <select 
              className="mt-1 block w-full border border-gray-300 rounded-md p-2"
              value={formData.channel}
              onChange={(e) => setFormData({...formData, channel: e.target.value})}
            >
              <option value="App">Thông báo ứng dụng</option>
              <option value="Email">Email</option>
            </select>
          </div>

          <button 
            type="submit"
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition"
          >
            Lưu cài đặt
          </button>
        </form>

        {status && (
          <div className={`mt-4 p-2 text-center rounded ${status.includes('✅') ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
            {status}
          </div>
        )}
      </div>
    </div>
  );
}