use std::time::Duration;

use serialport::FlowControl;

fn main() {
    //获取环境变量
    let args: Vec<String> = std::env::args().collect();
    let port = &args[1];
    let mut port = serialport::new(port, 115200)
    .timeout(Duration::from_millis(10))
    .flow_control(FlowControl::None)
    .open().unwrap();
    //防止之前没退出复位状态
    port.write_request_to_send(false).unwrap();
    port.write_data_terminal_ready(false).unwrap();
    std::thread::sleep(Duration::from_millis(50));

    port.write_data_terminal_ready(true).unwrap();
    port.write_request_to_send(false).unwrap();
    std::thread::sleep(Duration::from_millis(20));

    port.write_request_to_send(true).unwrap();
    port.write_data_terminal_ready(false).unwrap();
    std::thread::sleep(Duration::from_millis(10));

    port.write_request_to_send(false).unwrap();
    port.write_data_terminal_ready(true).unwrap();

    std::thread::sleep(Duration::from_millis(5));

    port.write_data_terminal_ready(false).unwrap();
}
